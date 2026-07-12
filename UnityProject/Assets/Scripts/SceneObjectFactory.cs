using System.Collections.Generic;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Builds every 3D object in the demo from built-in Unity primitives —
    /// no imported models, textures, or paid assets. Each recommended product's
    /// `sceneObjectType` maps to a builder here; adding a new object type for a
    /// new industry means adding one builder and one case to Build().
    ///
    /// WebGL note: everything shares a handful of cached Standard-shader
    /// materials, keeping draw-call state changes and memory low.
    /// </summary>
    public static class SceneObjectFactory
    {
        static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();
        static readonly Dictionary<PrimitiveType, Mesh> MeshCache = new Dictionary<PrimitiveType, Mesh>();
        static Shader _shader;

        static Shader BaseShader
        {
            get
            {
                if (_shader == null)
                {
                    _shader = Shader.Find("Standard");
                    if (_shader == null) _shader = Shader.Find("Legacy Shaders/Diffuse");
                    if (_shader == null) _shader = Shader.Find("Diffuse");
                }
                return _shader;
            }
        }

        // ---------------------------------------------------------------- API

        /// <summary>
        /// Builds the main (anchor) product for the page. Main products get a
        /// list of "colorable" renderers so variant swatches can retint them
        /// (sofa upholstery, appliance finish, statue base/accents).
        /// </summary>
        public static GameObject BuildMainProduct(string sceneObjectType, Color color, out List<Renderer> colorable)
        {
            switch (sceneObjectType)
            {
                case "refrigerator": return BuildRefrigerator(color, out colorable);
                case "statue": return BuildStatue(color, out colorable);
                case "sofa": return BuildSofa(color, out colorable);
                default:
                    Debug.LogWarning($"[SceneObjectFactory] No main-product builder for '{sceneObjectType}' — using sofa.");
                    return BuildSofa(color, out colorable);
            }
        }

        /// <summary>Builds the scene object for a recommended product based on its sceneObjectType.</summary>
        public static GameObject Build(RecommendedProduct item)
        {
            Color color = ParseColor(item.color, new Color(0.7f, 0.65f, 0.6f));
            GameObject go;
            List<Renderer> _;
            switch (item.sceneObjectType)
            {
                // Furniture
                case "coffee_table": go = BuildCoffeeTable(color); break;
                case "rug": go = BuildRug(color); break;
                case "floor_lamp": go = BuildFloorLamp(color); break;
                case "accent_chair": go = BuildAccentChair(color); break;
                case "side_table": go = BuildSideTable(color); break;
                case "wall_art": go = BuildWallArt(color); break;
                // Appliances (kitchen suite)
                case "refrigerator": go = BuildRefrigerator(color, out _); break;
                case "range": go = BuildRange(color); break;
                case "otr_microwave": go = BuildOtrMicrowave(color); break;
                case "dishwasher": go = BuildDishwasher(color); break;
                case "kitchen_island": go = BuildKitchenIsland(color); break;
                case "wall_cabinet": go = BuildWallCabinet(color); break;
                case "bar_stool": go = BuildBarStools(color); break;
                // Collectibles (display room)
                case "statue": go = BuildStatue(color, out _); break;
                case "display_pedestal": go = BuildDisplayPedestal(color); break;
                case "display_case": go = BuildDisplayCase(color); break;
                case "gallery_spotlight": go = BuildGallerySpotlight(color); break;
                case "collectible_bust": go = BuildCollectibleBust(color); break;
                case "wall_shelf": go = BuildWallShelf(color); break;
                default:
                    Debug.LogWarning($"[SceneObjectFactory] Unknown sceneObjectType '{item.sceneObjectType}' — using placeholder cube.");
                    go = new GameObject(item.sceneObjectType ?? "unknown");
                    Cube(go, new Vector3(0f, 0.25f, 0f), Vector3.one * 0.5f, color, "Placeholder");
                    break;
            }
            go.name = item.name;
            return go;
        }

        public static Color ParseColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            if (!hex.StartsWith("#")) hex = "#" + hex;
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : fallback;
        }

        public static Material CreateMaterial(Color color)
        {
            var mat = new Material(BaseShader) { color = color };
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.22f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            return mat;
        }

        /// <summary>Transparent Standard-shader material (display cases, glass).</summary>
        public static Material CreateGlassMaterial(Color tint, float alpha)
        {
            var mat = new Material(BaseShader);
            mat.color = new Color(tint.r, tint.g, tint.b, alpha);
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.75f);
            }
            return mat;
        }

        public static Material GetSharedMaterial(Color color)
        {
            string key = ColorUtility.ToHtmlStringRGB(color);
            if (!MaterialCache.TryGetValue(key, out var mat) || mat == null)
            {
                mat = CreateMaterial(color);
                MaterialCache[key] = mat;
            }
            return mat;
        }

        // ------------------------------------------------------------ Builders

        /// <summary>
        /// Sofa from grouped cubes: base, back, arms, seat/back cushions, feet.
        /// Returns the renderers whose color should change with upholstery swaps.
        /// </summary>
        public static GameObject BuildSofa(Color upholstery, out List<Renderer> upholsteryRenderers)
        {
            var root = new GameObject("Sofa");
            var rends = new List<Renderer>();
            Color cushion = Darken(upholstery, 0.88f);
            Color feet = new Color(0.28f, 0.21f, 0.16f);

            rends.Add(Cube(root, new Vector3(0f, 0.30f, 0f), new Vector3(2.2f, 0.28f, 0.95f), upholstery, "Base"));
            rends.Add(Cube(root, new Vector3(0f, 0.72f, -0.36f), new Vector3(2.2f, 0.75f, 0.24f), upholstery, "Back"));
            rends.Add(Cube(root, new Vector3(-1.02f, 0.62f, 0f), new Vector3(0.18f, 0.55f, 0.95f), upholstery, "ArmL"));
            rends.Add(Cube(root, new Vector3(1.02f, 0.62f, 0f), new Vector3(0.18f, 0.55f, 0.95f), upholstery, "ArmR"));

            for (int i = 0; i < 3; i++)
            {
                float x = -0.61f + i * 0.61f;
                rends.Add(Cube(root, new Vector3(x, 0.52f, 0.06f), new Vector3(0.58f, 0.16f, 0.78f), cushion, $"SeatCushion{i}"));
                rends.Add(Cube(root, new Vector3(x, 0.84f, -0.28f), new Vector3(0.56f, 0.42f, 0.16f), cushion, $"BackCushion{i}"));
            }

            foreach (float x in new[] { -0.95f, 0.95f })
            foreach (float z in new[] { -0.38f, 0.38f })
            {
                Cube(root, new Vector3(x, 0.08f, z), new Vector3(0.08f, 0.16f, 0.08f), feet, "Foot");
            }

            upholsteryRenderers = rends;
            return root;
        }

        static GameObject BuildCoffeeTable(Color wood)
        {
            var root = new GameObject("CoffeeTable");
            Color legColor = Darken(wood, 0.8f);
            Cube(root, new Vector3(0f, 0.42f, 0f), new Vector3(1.15f, 0.05f, 0.6f), wood, "Top");
            foreach (float x in new[] { -0.52f, 0.52f })
            foreach (float z in new[] { -0.24f, 0.24f })
            {
                Cube(root, new Vector3(x, 0.2f, z), new Vector3(0.05f, 0.4f, 0.05f), legColor, "Leg");
            }
            return root;
        }

        static GameObject BuildRug(Color color)
        {
            var root = new GameObject("Rug");
            var rug = Cube(root, new Vector3(0f, 0.01f, 0f), new Vector3(2.9f, 0.02f, 1.9f), color, "Rug");
            rug.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Inner band for a simple woven-border look.
            var inner = Cube(root, new Vector3(0f, 0.021f, 0f), new Vector3(2.5f, 0.012f, 1.5f), Darken(color, 0.9f), "InnerBand");
            inner.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return root;
        }

        static GameObject BuildFloorLamp(Color metal)
        {
            var root = new GameObject("FloorLamp");
            Color shade = new Color(0.94f, 0.88f, 0.76f);
            Cylinder(root, new Vector3(0f, 0.015f, 0f), new Vector3(0.36f, 0.015f, 0.36f), metal, "Base");
            Cylinder(root, new Vector3(0f, 0.75f, 0f), new Vector3(0.05f, 0.72f, 0.05f), metal, "Pole");
            Cylinder(root, new Vector3(0f, 1.52f, 0f), new Vector3(0.34f, 0.14f, 0.34f), shade, "Shade");

            // One soft point light sells the "cozy corner" without heavy cost.
            var lightGo = new GameObject("LampLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.88f, 0.7f);
            light.intensity = 0.7f;
            light.range = 3.5f;
            light.shadows = LightShadows.None;
            return root;
        }

        static GameObject BuildAccentChair(Color fabric)
        {
            var root = new GameObject("AccentChair");
            Color cushion = Darken(fabric, 0.88f);
            Color feet = new Color(0.28f, 0.21f, 0.16f);
            Cube(root, new Vector3(0f, 0.3f, 0f), new Vector3(0.8f, 0.24f, 0.75f), fabric, "Base");
            Cube(root, new Vector3(0f, 0.48f, 0.03f), new Vector3(0.7f, 0.14f, 0.62f), cushion, "SeatCushion");
            Cube(root, new Vector3(0f, 0.72f, -0.3f), new Vector3(0.8f, 0.62f, 0.16f), fabric, "Back");
            Cube(root, new Vector3(-0.44f, 0.56f, 0f), new Vector3(0.12f, 0.38f, 0.7f), fabric, "ArmL");
            Cube(root, new Vector3(0.44f, 0.56f, 0f), new Vector3(0.12f, 0.38f, 0.7f), fabric, "ArmR");
            foreach (float x in new[] { -0.32f, 0.32f })
            foreach (float z in new[] { -0.28f, 0.28f })
            {
                Cube(root, new Vector3(x, 0.09f, z), new Vector3(0.07f, 0.18f, 0.07f), feet, "Foot");
            }
            return root;
        }

        static GameObject BuildSideTable(Color wood)
        {
            var root = new GameObject("SideTable");
            Color stem = Darken(wood, 0.82f);
            Cylinder(root, new Vector3(0f, 0.5f, 0f), new Vector3(0.48f, 0.02f, 0.48f), wood, "Top");
            Cylinder(root, new Vector3(0f, 0.26f, 0f), new Vector3(0.06f, 0.24f, 0.06f), stem, "Stem");
            Cylinder(root, new Vector3(0f, 0.015f, 0f), new Vector3(0.3f, 0.015f, 0.3f), stem, "Base");
            return root;
        }

        static GameObject BuildWallArt(Color accent)
        {
            var root = new GameObject("WallArt");
            Color frame = new Color(0.24f, 0.19f, 0.15f);
            Color canvas = new Color(0.95f, 0.93f, 0.88f);
            Cube(root, new Vector3(0f, 0f, 0f), new Vector3(1.15f, 0.85f, 0.05f), frame, "Frame");
            Cube(root, new Vector3(0f, 0f, 0.02f), new Vector3(1.03f, 0.73f, 0.03f), canvas, "Canvas");
            // Abstract blocks in the accent color family.
            Cube(root, new Vector3(-0.2f, 0.08f, 0.045f), new Vector3(0.42f, 0.34f, 0.012f), accent, "Block1");
            Cube(root, new Vector3(0.24f, -0.14f, 0.045f), new Vector3(0.3f, 0.26f, 0.012f), Darken(accent, 0.72f), "Block2");
            return root;
        }

        // ----------------------------------------- Builders: appliances demo

        /// <summary>French-door refrigerator; doors/drawer take the finish color.</summary>
        public static GameObject BuildRefrigerator(Color finish, out List<Renderer> colorable)
        {
            var root = new GameObject("Refrigerator");
            var rends = new List<Renderer>();
            Color handle = new Color(0.2f, 0.21f, 0.23f);

            rends.Add(Cube(root, new Vector3(0f, 0.88f, 0f), new Vector3(0.9f, 1.74f, 0.7f), finish, "Body"));
            rends.Add(Cube(root, new Vector3(-0.23f, 1.23f, 0.36f), new Vector3(0.42f, 1.0f, 0.04f), finish, "DoorL"));
            rends.Add(Cube(root, new Vector3(0.23f, 1.23f, 0.36f), new Vector3(0.42f, 1.0f, 0.04f), finish, "DoorR"));
            rends.Add(Cube(root, new Vector3(0f, 0.44f, 0.36f), new Vector3(0.86f, 0.5f, 0.04f), finish, "FreezerDrawer"));

            Cube(root, new Vector3(-0.055f, 1.22f, 0.395f), new Vector3(0.03f, 0.7f, 0.03f), handle, "HandleL");
            Cube(root, new Vector3(0.055f, 1.22f, 0.395f), new Vector3(0.03f, 0.7f, 0.03f), handle, "HandleR");
            Cube(root, new Vector3(0f, 0.62f, 0.395f), new Vector3(0.5f, 0.035f, 0.03f), handle, "DrawerHandle");

            colorable = rends;
            return root;
        }

        static GameObject BuildRange(Color finish)
        {
            var root = new GameObject("Range");
            Color dark = new Color(0.15f, 0.15f, 0.17f);
            Color darker = new Color(0.09f, 0.09f, 0.1f);
            Cube(root, new Vector3(0f, 0.44f, 0f), new Vector3(0.76f, 0.86f, 0.66f), finish, "Body");
            Cube(root, new Vector3(0f, 0.89f, 0f), new Vector3(0.76f, 0.04f, 0.66f), dark, "Cooktop");
            foreach (float x in new[] { -0.19f, 0.19f })
            foreach (float z in new[] { -0.16f, 0.16f })
            {
                Cylinder(root, new Vector3(x, 0.915f, z), new Vector3(0.15f, 0.006f, 0.15f), darker, "Burner");
            }
            Cube(root, new Vector3(0f, 0.38f, 0.345f), new Vector3(0.68f, 0.5f, 0.03f), finish, "OvenDoor");
            Cube(root, new Vector3(0f, 0.4f, 0.365f), new Vector3(0.46f, 0.26f, 0.015f), darker, "OvenWindow");
            Cube(root, new Vector3(0f, 0.68f, 0.37f), new Vector3(0.68f, 0.035f, 0.035f), dark, "Handle");
            return root;
        }

        /// <summary>Over-the-range microwave; pivot at its own center (wall-mounted).</summary>
        static GameObject BuildOtrMicrowave(Color finish)
        {
            var root = new GameObject("OtrMicrowave");
            Color dark = new Color(0.12f, 0.12f, 0.14f);
            Cube(root, new Vector3(0f, 0f, 0f), new Vector3(0.76f, 0.4f, 0.38f), finish, "Body");
            Cube(root, new Vector3(-0.08f, 0f, 0.195f), new Vector3(0.5f, 0.3f, 0.015f), dark, "DoorWindow");
            Cube(root, new Vector3(0.3f, 0f, 0.2f), new Vector3(0.03f, 0.3f, 0.02f), dark, "Handle");
            return root;
        }

        static GameObject BuildDishwasher(Color finish)
        {
            var root = new GameObject("Dishwasher");
            Color dark = new Color(0.2f, 0.21f, 0.23f);
            Cube(root, new Vector3(0f, 0.43f, 0f), new Vector3(0.6f, 0.84f, 0.6f), finish, "Body");
            Cube(root, new Vector3(0f, 0.38f, 0.315f), new Vector3(0.56f, 0.66f, 0.03f), Darken(finish, 0.94f), "FrontPanel");
            Cube(root, new Vector3(0f, 0.78f, 0.33f), new Vector3(0.56f, 0.035f, 0.035f), dark, "Handle");
            return root;
        }

        static GameObject BuildKitchenIsland(Color wood)
        {
            var root = new GameObject("KitchenIsland");
            Color cabinet = new Color(0.91f, 0.9f, 0.88f);
            Color kick = new Color(0.25f, 0.25f, 0.27f);
            Cube(root, new Vector3(0f, 0.9f, 0f), new Vector3(1.5f, 0.06f, 0.8f), wood, "Top");
            Cube(root, new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.82f, 0.72f), cabinet, "Base");
            Cube(root, new Vector3(0f, 0.04f, 0f), new Vector3(1.3f, 0.08f, 0.64f), kick, "ToeKick");
            return root;
        }

        /// <summary>Pair of wall cabinets; pivot at group center (wall-mounted).</summary>
        static GameObject BuildWallCabinet(Color color)
        {
            var root = new GameObject("WallCabinet");
            Color handle = new Color(0.3f, 0.31f, 0.33f);
            foreach (float x in new[] { -0.34f, 0.34f })
            {
                Cube(root, new Vector3(x, 0f, 0f), new Vector3(0.64f, 0.7f, 0.34f), color, "Cabinet");
                Cube(root, new Vector3(x + (x < 0 ? 0.24f : -0.24f), -0.2f, 0.18f), new Vector3(0.025f, 0.16f, 0.02f), handle, "Handle");
            }
            return root;
        }

        static GameObject BuildBarStools(Color wood)
        {
            var root = new GameObject("BarStools");
            foreach (float x in new[] { -0.45f, 0.45f })
            {
                Cylinder(root, new Vector3(x, 0.66f, 0f), new Vector3(0.34f, 0.03f, 0.34f), wood, "Seat");
                Cylinder(root, new Vector3(x, 0.33f, 0f), new Vector3(0.05f, 0.3f, 0.05f), Darken(wood, 0.8f), "Pole");
                Cylinder(root, new Vector3(x, 0.015f, 0f), new Vector3(0.28f, 0.015f, 0.28f), Darken(wood, 0.8f), "Base");
            }
            return root;
        }

        // --------------------------------------- Builders: collectibles demo

        /// <summary>
        /// Abstract heroic figure — no real character likeness. The plinth,
        /// cape, and accents take the "base finish" variant color.
        /// </summary>
        public static GameObject BuildStatue(Color baseColor, out List<Renderer> colorable)
        {
            var root = new GameObject("Statue");
            var rends = new List<Renderer>();
            Color steel = new Color(0.42f, 0.45f, 0.5f);
            Color steelLight = new Color(0.5f, 0.53f, 0.58f);
            Color blade = new Color(0.78f, 0.8f, 0.83f);

            rends.Add(Cylinder(root, new Vector3(0f, 0.04f, 0f), new Vector3(0.4f, 0.04f, 0.4f), baseColor, "Plinth"));
            Cube(root, new Vector3(0f, 0.29f, 0f), new Vector3(0.2f, 0.42f, 0.15f), steel, "Legs");
            Cube(root, new Vector3(0f, 0.62f, 0f), new Vector3(0.28f, 0.32f, 0.17f), steelLight, "Torso");
            rends.Add(Cube(root, new Vector3(0f, 0.58f, -0.12f), new Vector3(0.36f, 0.55f, 0.05f), baseColor, "Cape"));
            Shape(PrimitiveType.Sphere, root, new Vector3(0f, 0.9f, 0f), new Vector3(0.16f, 0.16f, 0.16f), steelLight, "Head");
            rends.Add(Cube(root, new Vector3(-0.19f, 0.78f, 0f), new Vector3(0.1f, 0.08f, 0.12f), baseColor, "PauldronL"));
            rends.Add(Cube(root, new Vector3(0.19f, 0.78f, 0f), new Vector3(0.1f, 0.08f, 0.12f), baseColor, "PauldronR"));
            Cylinder(root, new Vector3(0.21f, 0.86f, 0.02f), new Vector3(0.045f, 0.12f, 0.045f), steel, "RaisedArm");
            rends.Add(Cube(root, new Vector3(0.21f, 1.0f, 0.02f), new Vector3(0.1f, 0.03f, 0.03f), baseColor, "SwordHilt"));
            Cube(root, new Vector3(0.21f, 1.28f, 0.02f), new Vector3(0.035f, 0.52f, 0.02f), blade, "SwordBlade");

            colorable = rends;
            return root;
        }

        static GameObject BuildDisplayPedestal(Color color)
        {
            var root = new GameObject("DisplayPedestal");
            Cube(root, new Vector3(0f, 0.43f, 0f), new Vector3(0.55f, 0.86f, 0.55f), color, "Column");
            Cube(root, new Vector3(0f, 0.875f, 0f), new Vector3(0.62f, 0.03f, 0.62f), Lighten(color, 1.6f), "TopPlate");
            return root;
        }

        /// <summary>Transparent acrylic case; pivot at its floor/base level.</summary>
        static GameObject BuildDisplayCase(Color tint)
        {
            var root = new GameObject("DisplayCase");
            Color frame = new Color(0.16f, 0.16f, 0.18f);
            Cube(root, new Vector3(0f, 0.025f, 0f), new Vector3(0.9f, 0.05f, 0.9f), frame, "BaseFrame");

            foreach (float x in new[] { -0.43f, 0.43f })
            foreach (float z in new[] { -0.43f, 0.43f })
            {
                Cube(root, new Vector3(x, 0.84f, z), new Vector3(0.035f, 1.58f, 0.035f), frame, "CornerPost");
            }

            foreach (float z in new[] { -0.43f, 0.43f })
            {
                Cube(root, new Vector3(0f, 1.63f, z), new Vector3(0.9f, 0.035f, 0.035f), frame, "TopRail");
            }
            foreach (float x in new[] { -0.43f, 0.43f })
            {
                Cube(root, new Vector3(x, 1.63f, 0f), new Vector3(0.035f, 0.035f, 0.9f), frame, "TopRail");
            }

            var glassMat = CreateGlassMaterial(tint, 0.08f);
            AddGlassPanel(root, new Vector3(0f, 0.85f, -0.43f), new Vector3(0.82f, 1.48f, 0.018f), glassMat, "BackGlass");
            AddGlassPanel(root, new Vector3(-0.43f, 0.85f, 0f), new Vector3(0.018f, 1.48f, 0.82f), glassMat, "SideGlassL");
            AddGlassPanel(root, new Vector3(0.43f, 0.85f, 0f), new Vector3(0.018f, 1.48f, 0.82f), glassMat, "SideGlassR");
            AddGlassPanel(root, new Vector3(0f, 1.62f, 0f), new Vector3(0.82f, 0.018f, 0.82f), glassMat, "TopGlass");
            return root;
        }

        static GameObject BuildGallerySpotlight(Color metal)
        {
            var root = new GameObject("GallerySpotlight");
            Cylinder(root, new Vector3(0f, 0.015f, 0f), new Vector3(0.3f, 0.015f, 0.3f), metal, "Base");
            Cylinder(root, new Vector3(0f, 0.62f, 0f), new Vector3(0.045f, 0.6f, 0.045f), metal, "Pole");

            var head = ShapeObject(PrimitiveType.Cylinder, root, new Vector3(0.08f, 1.24f, 0.06f), new Vector3(0.11f, 0.1f, 0.11f), "Head");
            head.transform.localRotation = Quaternion.Euler(0f, 0f, -55f);
            head.GetComponent<Renderer>().sharedMaterial = GetSharedMaterial(Darken(metal, 0.85f));

            var lightGo = new GameObject("SpotLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0.14f, 1.22f, 0.08f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.95f, 0.85f);
            light.intensity = 1.1f;
            light.range = 4f;
            light.shadows = LightShadows.None;
            return root;
        }

        static GameObject BuildCollectibleBust(Color material)
        {
            var root = new GameObject("CollectibleBust");
            Color column = new Color(0.17f, 0.17f, 0.19f);
            Cube(root, new Vector3(0f, 0.375f, 0f), new Vector3(0.42f, 0.75f, 0.42f), column, "Column");
            Cube(root, new Vector3(0f, 0.77f, 0f), new Vector3(0.46f, 0.03f, 0.46f), Lighten(column, 1.8f), "TopPlate");
            Cube(root, new Vector3(0f, 0.86f, 0f), new Vector3(0.3f, 0.14f, 0.18f), material, "Shoulders");
            Shape(PrimitiveType.Sphere, root, new Vector3(0f, 1.0f, 0f), new Vector3(0.17f, 0.17f, 0.17f), Lighten(material, 1.12f), "Head");
            return root;
        }

        /// <summary>Two floating shelves with small display pieces; pivot at top shelf (wall-mounted).</summary>
        static GameObject BuildWallShelf(Color color)
        {
            var root = new GameObject("WallShelf");
            Color accent = new Color(0.62f, 0.17f, 0.25f);
            Color piece = new Color(0.55f, 0.57f, 0.62f);
            foreach (float y in new[] { 0f, -0.45f })
            {
                Cube(root, new Vector3(0f, y, 0f), new Vector3(1.1f, 0.04f, 0.24f), color, "Board");
            }
            Cube(root, new Vector3(-0.32f, 0.1f, 0f), new Vector3(0.12f, 0.16f, 0.12f), piece, "MiniPiece1");
            Cylinder(root, new Vector3(0.05f, 0.09f, 0f), new Vector3(0.1f, 0.07f, 0.1f), accent, "MiniPiece2");
            Cube(root, new Vector3(0.36f, 0.08f, 0f), new Vector3(0.1f, 0.12f, 0.1f), piece, "MiniPiece3");
            Cube(root, new Vector3(-0.1f, -0.36f, 0f), new Vector3(0.14f, 0.14f, 0.12f), piece, "MiniPiece4");
            Cylinder(root, new Vector3(0.3f, -0.37f, 0f), new Vector3(0.09f, 0.06f, 0.09f), accent, "MiniPiece5");
            return root;
        }

        // ------------------------------------------------------------ Helpers

        static Color Lighten(Color c, float factor)
        {
            return new Color(Mathf.Clamp01(c.r * factor), Mathf.Clamp01(c.g * factor), Mathf.Clamp01(c.b * factor), 1f);
        }

        public static Renderer Cube(GameObject parent, Vector3 localPos, Vector3 localScale, Color color, string name)
        {
            return Shape(PrimitiveType.Cube, parent, localPos, localScale, color, name);
        }

        public static Renderer Cylinder(GameObject parent, Vector3 localPos, Vector3 localScale, Color color, string name)
        {
            return Shape(PrimitiveType.Cylinder, parent, localPos, localScale, color, name);
        }

        static void AddGlassPanel(GameObject parent, Vector3 localPos, Vector3 localScale, Material material, string name)
        {
            var renderer = Cube(parent, localPos, localScale, Color.white, name);
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Renderer Shape(PrimitiveType type, GameObject parent, Vector3 localPos, Vector3 localScale, Color color, string name)
        {
            var go = ShapeObject(type, parent, localPos, localScale, name);
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = GetSharedMaterial(color);
            return renderer;
        }

        static GameObject ShapeObject(PrimitiveType type, GameObject parent, Vector3 localPos, Vector3 localScale, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;

            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = GetPrimitiveMesh(type);
            go.AddComponent<MeshRenderer>();
            return go;
        }

        static Mesh GetPrimitiveMesh(PrimitiveType type)
        {
            if (MeshCache.TryGetValue(type, out var cached) && cached != null) return cached;

            var mesh = type switch
            {
                PrimitiveType.Cylinder => BuildCylinderMesh(),
                PrimitiveType.Sphere => BuildSphereMesh(),
                _ => BuildCubeMesh()
            };

            MeshCache[type] = mesh;
            return mesh;
        }

        static Mesh BuildCubeMesh()
        {
            var mesh = new Mesh { name = "RuntimeCube" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.5f, 0.5f, -0.5f), new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f)
            };
            mesh.triangles = new[]
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
                8, 9, 10, 8, 10, 11,
                12, 13, 14, 12, 14, 15,
                16, 17, 18, 16, 18, 19,
                20, 21, 22, 20, 22, 23
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildCylinderMesh()
        {
            const int segments = 32;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle) * 0.5f;
                float z = Mathf.Sin(angle) * 0.5f;
                vertices.Add(new Vector3(x, -1f, z));
                vertices.Add(new Vector3(x, 1f, z));
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int b0 = i * 2;
                int t0 = b0 + 1;
                int b1 = next * 2;
                int t1 = b1 + 1;
                triangles.AddRange(new[] { b0, t0, t1, b0, t1, b1 });
            }

            int topCenter = vertices.Count;
            vertices.Add(new Vector3(0f, 1f, 0f));
            int topStart = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * 0.5f, 1f, Mathf.Sin(angle) * 0.5f));
            }

            int bottomCenter = vertices.Count;
            vertices.Add(new Vector3(0f, -1f, 0f));
            int bottomStart = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices.Add(new Vector3(Mathf.Cos(angle) * 0.5f, -1f, Mathf.Sin(angle) * 0.5f));
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                triangles.AddRange(new[] { topCenter, topStart + i, topStart + next });
                triangles.AddRange(new[] { bottomCenter, bottomStart + next, bottomStart + i });
            }

            var mesh = new Mesh { name = "RuntimeCylinder" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh BuildSphereMesh()
        {
            const int latitudes = 12;
            const int longitudes = 24;
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (int lat = 0; lat <= latitudes; lat++)
            {
                float theta = Mathf.PI * lat / latitudes;
                float y = Mathf.Cos(theta) * 0.5f;
                float radius = Mathf.Sin(theta) * 0.5f;
                for (int lon = 0; lon <= longitudes; lon++)
                {
                    float phi = Mathf.PI * 2f * lon / longitudes;
                    vertices.Add(new Vector3(Mathf.Cos(phi) * radius, y, Mathf.Sin(phi) * radius));
                }
            }

            int row = longitudes + 1;
            for (int lat = 0; lat < latitudes; lat++)
            {
                for (int lon = 0; lon < longitudes; lon++)
                {
                    int a = lat * row + lon;
                    int b = a + row;
                    triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            }

            var mesh = new Mesh { name = "RuntimeSphere" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static Color Darken(Color c, float factor)
        {
            return new Color(c.r * factor, c.g * factor, c.b * factor, 1f);
        }
    }
}
