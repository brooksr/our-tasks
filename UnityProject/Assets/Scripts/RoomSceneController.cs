using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Owns the room structure (floor + walls) and every product object in it.
    /// Placement is driven by each product's sceneObjectType and the active
    /// room-size preset, so different retailer data lays itself out without
    /// touching this code. Room sizes adjust dimensions, object spacing, and
    /// camera framing — the "Apartment / Standard / Large" selector.
    /// </summary>
    public class RoomSceneController : MonoBehaviour
    {
        class RoomSpec
        {
            public string id;
            public float width;
            public float depth;
            public float spacing;
            public float cameraDistance;
        }

        static readonly Dictionary<string, RoomSpec> RoomSpecs = new Dictionary<string, RoomSpec>
        {
            { "apartment", new RoomSpec { id = "apartment", width = 4.4f, depth = 3.6f, spacing = 0.8f, cameraDistance = 5.4f } },
            { "standard", new RoomSpec { id = "standard", width = 5.6f, depth = 4.4f, spacing = 1.0f, cameraDistance = 6.6f } },
            { "large", new RoomSpec { id = "large", width = 7.2f, depth = 5.4f, spacing = 1.18f, cameraDistance = 8.0f } },
        };

        const float WallHeight = 2.6f;
        const float WallThickness = 0.1f;

        CommerceAppController _app;
        GameObject _roomRoot;
        GameObject _structureRoot;
        readonly Dictionary<string, ProductSceneObject> _objects = new Dictionary<string, ProductSceneObject>();
        RoomSpec _spec;

        CommerceAppController App => _app != null ? _app : (_app = GetComponent<CommerceAppController>());

        public string CurrentRoomSize => _spec?.id ?? "standard";

        // ---------------------------------------------------------------- API

        public void BuildRoom(RetailerConfig config, Product mainProduct, RecommendationSet recommendations)
        {
            Clear();
            _spec = ResolveSpec(config?.scenePreset?.roomPreset);
            _roomRoot = new GameObject("Room");

            BuildStructure(config);
            SpawnMainProduct(mainProduct);
            SpawnRecommendedObjects(recommendations);
            RelayoutObjects();
            UpdateCameraFraming();
        }

        public void SetRoomSize(string size, bool emitEvent = true)
        {
            var spec = ResolveSpec(size);
            if (_roomRoot == null)
            {
                _spec = spec;
                return;
            }

            _spec = spec;
            if (_structureRoot != null) Destroy(_structureRoot);
            BuildStructure(App.Config);
            RelayoutObjects();
            UpdateCameraFraming();

            if (emitEvent)
            {
                App.Bridge.Emit(AnalyticsEvent.RoomSizeChanged, AnalyticsEvent.Payload(("roomSize", _spec.id)));
            }
        }

        public void SetObjectVisible(string productId, bool visible)
        {
            if (_objects.TryGetValue(productId, out var obj))
            {
                obj.SetVisible(visible);

                // Objects that stack on the pedestal (statue, display case)
                // ride up/down when the pedestal is toggled.
                if (obj.sceneObjectType == "display_pedestal")
                {
                    foreach (var stacked in _objects.Values.Where(
                                 o => o.sceneObjectType == "statue" || o.sceneObjectType == "display_case"))
                    {
                        PlaceObject(stacked);
                    }
                }
            }
        }

        bool IsTypeVisible(string sceneObjectType)
        {
            return _objects.Values.Any(o => o.sceneObjectType == sceneObjectType && o.gameObject.activeSelf);
        }

        void RelayoutObjects()
        {
            foreach (var obj in _objects.Values)
            {
                PlaceObject(obj);
            }
        }

        public void Clear()
        {
            if (_roomRoot != null) Destroy(_roomRoot);
            _roomRoot = null;
            _structureRoot = null;
            _objects.Clear();
        }

        // ------------------------------------------------------------ Internal

        static RoomSpec ResolveSpec(string id)
        {
            if (!string.IsNullOrEmpty(id) && RoomSpecs.TryGetValue(id, out var spec)) return spec;
            if (!string.IsNullOrEmpty(id)) Debug.LogWarning($"[RoomScene] Unknown room preset '{id}', using 'standard'.");
            return RoomSpecs["standard"];
        }

        void BuildStructure(RetailerConfig config)
        {
            _structureRoot = new GameObject("Structure");
            _structureRoot.transform.SetParent(_roomRoot.transform, false);

            Color floorColor = SceneObjectFactory.ParseColor(config?.scenePreset?.floorColor, new Color(0.69f, 0.55f, 0.42f));
            Color wallColor = SceneObjectFactory.ParseColor(config?.scenePreset?.wallColor, new Color(0.94f, 0.91f, 0.86f));

            // Floor
            SceneObjectFactory.Cube(_structureRoot,
                new Vector3(0f, -0.05f, 0f),
                new Vector3(_spec.width, 0.1f, _spec.depth),
                floorColor, "Floor");

            // Back wall (behind the sofa)
            SceneObjectFactory.Cube(_structureRoot,
                new Vector3(0f, WallHeight / 2f, -_spec.depth / 2f - WallThickness / 2f),
                new Vector3(_spec.width, WallHeight, WallThickness),
                wallColor, "BackWall");

            // Side wall (left of the seating area)
            SceneObjectFactory.Cube(_structureRoot,
                new Vector3(-_spec.width / 2f - WallThickness / 2f, WallHeight / 2f, 0f),
                new Vector3(WallThickness, WallHeight, _spec.depth),
                Lighten(wallColor, 1.03f), "SideWall");

            // Baseboards for a subtle showroom finish.
            Color baseboard = Lighten(wallColor, 1.08f);
            SceneObjectFactory.Cube(_structureRoot,
                new Vector3(0f, 0.06f, -_spec.depth / 2f + 0.02f),
                new Vector3(_spec.width, 0.12f, 0.04f),
                baseboard, "BaseboardBack");
            SceneObjectFactory.Cube(_structureRoot,
                new Vector3(-_spec.width / 2f + 0.02f, 0.06f, 0f),
                new Vector3(0.04f, 0.12f, _spec.depth),
                baseboard, "BaseboardSide");
        }

        void SpawnMainProduct(Product mainProduct)
        {
            string defaultHex = (mainProduct.variants != null && mainProduct.variants.Count > 0)
                ? mainProduct.variants[0].color
                : "#C9B79C";
            Color color = SceneObjectFactory.ParseColor(defaultHex, new Color(0.79f, 0.72f, 0.61f));
            string type = string.IsNullOrEmpty(mainProduct.sceneObjectType) ? "sofa" : mainProduct.sceneObjectType;

            var main = SceneObjectFactory.BuildMainProduct(type, color, out var colorable);
            main.transform.SetParent(_roomRoot.transform, false);

            var pso = main.AddComponent<ProductSceneObject>();
            pso.productId = mainProduct.id;
            pso.category = mainProduct.category;
            pso.sceneObjectType = type;
            _objects[mainProduct.id] = pso;
            PlaceObject(pso);

            App.Sofa.Bind(colorable, defaultHex);
        }

        void SpawnRecommendedObjects(RecommendationSet recommendations)
        {
            if (recommendations?.items == null) return;
            foreach (var item in recommendations.items)
            {
                var go = SceneObjectFactory.Build(item);
                go.transform.SetParent(_roomRoot.transform, false);

                var pso = go.AddComponent<ProductSceneObject>();
                pso.productId = item.id;
                pso.category = item.category;
                pso.sceneObjectType = item.sceneObjectType;
                _objects[item.id] = pso;
                PlaceObject(pso);
            }
        }

        /// <summary>
        /// Showroom layout anchors, scaled by the room preset's spacing factor.
        /// Wall art tracks the actual back wall rather than the spacing factor.
        /// </summary>
        void PlaceObject(ProductSceneObject obj)
        {
            var t = obj.transform;
            float s = _spec.spacing;
            float wallZ = -_spec.depth / 2f;
            float displayBaseY = IsTypeVisible("display_pedestal") ? 0.89f : 0f;
            t.localRotation = Quaternion.identity;

            switch (obj.sceneObjectType)
            {
                case "sofa":
                    t.localPosition = new Vector3(0f, 0f, -1.35f * s);
                    break;
                case "rug":
                    t.localPosition = new Vector3(0f, 0f, -0.35f * s);
                    break;
                case "coffee_table":
                    t.localPosition = new Vector3(0f, 0f, 0.15f * s);
                    break;
                case "floor_lamp":
                    t.localPosition = new Vector3(-1.75f * s, 0f, -1.2f * s);
                    break;
                case "side_table":
                    t.localPosition = new Vector3(1.55f * s, 0f, -1.25f * s);
                    break;
                case "accent_chair":
                    t.localPosition = new Vector3(1.9f * s, 0f, 0.85f * s);
                    t.localRotation = Quaternion.Euler(0f, 215f, 0f);
                    break;
                case "wall_art":
                    t.localPosition = new Vector3(0f, 1.78f, wallZ + 0.08f);
                    break;
                case "refrigerator":
                    t.localPosition = new Vector3(-1.9f * s, 0f, wallZ + 0.42f);
                    break;
                case "range":
                    t.localPosition = new Vector3(-0.5f * s, 0f, wallZ + 0.38f);
                    break;
                case "otr_microwave":
                    t.localPosition = new Vector3(-0.5f * s, 1.6f, wallZ + 0.22f);
                    break;
                case "dishwasher":
                    t.localPosition = new Vector3(0.45f * s, 0f, wallZ + 0.35f);
                    break;
                case "wall_cabinet":
                    t.localPosition = new Vector3(1.3f * s, 1.72f, wallZ + 0.19f);
                    break;
                case "kitchen_island":
                    t.localPosition = new Vector3(0f, 0f, 0.55f * s);
                    break;
                case "bar_stool":
                    t.localPosition = new Vector3(0f, 0f, 1.35f * s);
                    break;
                case "display_pedestal":
                    t.localPosition = new Vector3(0f, 0f, -1.05f * s);
                    break;
                case "statue":
                case "display_case":
                    t.localPosition = new Vector3(0f, displayBaseY, -1.05f * s);
                    break;
                case "gallery_spotlight":
                    t.localPosition = new Vector3(-1.5f * s, 0f, -0.95f * s);
                    break;
                case "collectible_bust":
                    t.localPosition = new Vector3(1.45f * s, 0f, -1.1f * s);
                    break;
                case "wall_shelf":
                    t.localPosition = new Vector3(-1.3f * s, 1.5f, wallZ + 0.16f);
                    break;
                default:
                    t.localPosition = Vector3.zero;
                    break;
            }
        }

        void UpdateCameraFraming()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var orbit = cam.GetComponent<CameraOrbitController>();
            if (orbit != null)
            {
                orbit.SetFraming(new Vector3(0f, 0.75f, -0.4f), _spec.cameraDistance);
            }
        }

        static Color Lighten(Color c, float factor)
        {
            return new Color(Mathf.Clamp01(c.r * factor), Mathf.Clamp01(c.g * factor), Mathf.Clamp01(c.b * factor), 1f);
        }
    }
}
