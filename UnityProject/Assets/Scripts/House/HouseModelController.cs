using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Runtime-safe control surface for the scanned-plan house model. The public
    /// methods are intentionally simple so a future WebGL bridge can call them
    /// without knowing the generated hierarchy.
    /// </summary>
    public class HouseModelController : MonoBehaviour
    {
        [SerializeField] GameObject roofRoot;
        [SerializeField] GameObject secondFloorRoot;
        [SerializeField] GameObject firstFloorRoot;
        [SerializeField] GameObject[] interiorWallGroups;
        [SerializeField] GameObject[] exteriorShellGroups;
        [SerializeField] GameObject[] cutawayWallGroups;
        [SerializeField] GameObject[] scanGroups;
        [SerializeField] GameObject proceduralFinishesRoot;
        [SerializeField] Camera perspectiveCamera;
        [SerializeField] Camera topDownCamera;

        Vector3 _roofBasePosition;
        Vector3 _secondFloorBasePosition;
        bool _baseTransformsCaptured;
        bool _roofRequestedVisible = true;
        bool _secondFloorRequestedVisible = true;
        bool _interiorRequestedVisible = true;
        bool _exteriorRequestedVisible = true;
        bool _scanRequestedVisible = true;
        bool _cutawayMode;
        float _explodedAmount;

        void Awake()
        {
            ResolveMissingReferences();
            CaptureBaseTransforms();
        }

        public void Bind(
            GameObject roofs,
            GameObject secondFloor,
            GameObject firstFloor,
            GameObject[] interiorGroups,
            GameObject[] exteriorGroups,
            GameObject[] cutawayGroups,
            GameObject[] scans,
            GameObject proceduralFinishes,
            Camera perspective,
            Camera topDown)
        {
            roofRoot = roofs;
            secondFloorRoot = secondFloor;
            firstFloorRoot = firstFloor;
            interiorWallGroups = interiorGroups;
            exteriorShellGroups = exteriorGroups;
            cutawayWallGroups = cutawayGroups;
            scanGroups = scans;
            proceduralFinishesRoot = proceduralFinishes;
            perspectiveCamera = perspective;
            topDownCamera = topDown;
            _baseTransformsCaptured = false;
            CaptureBaseTransforms();
            ApplyAllStates();
        }

        public void SetRoofVisible(bool visible)
        {
            _roofRequestedVisible = visible;
            ApplyRoofVisibility();
        }

        public void SetSecondFloorVisible(bool visible)
        {
            _secondFloorRequestedVisible = visible;
            SetActive(secondFloorRoot, visible);
        }

        public void SetInteriorWallsVisible(bool visible)
        {
            _interiorRequestedVisible = visible;
            SetActive(interiorWallGroups, visible);
        }

        public void SetExteriorShellVisible(bool visible)
        {
            _exteriorRequestedVisible = visible;
            SetActive(exteriorShellGroups, visible);
            ApplyRoofVisibility();
            ApplyCutawayVisibility();
        }

        public void SetCutawayMode(bool enabled)
        {
            _cutawayMode = enabled;
            ApplyRoofVisibility();
            ApplyCutawayVisibility();
            ApplyExplodedPositions();
        }

        // Shows the as-built scan meshes and hides the procedural finish set (or
        // vice-versa) so the two never overlap.
        public void SetScanVisible(bool visible)
        {
            _scanRequestedVisible = visible;
            SetActive(scanGroups, visible);
            SetActive(proceduralFinishesRoot, !visible);
        }

        public void SetExplodedView(float amount)
        {
            _explodedAmount = Mathf.Clamp01(amount);
            ApplyExplodedPositions();
        }

        public void ResetHouseView()
        {
            _roofRequestedVisible = true;
            _secondFloorRequestedVisible = true;
            _interiorRequestedVisible = true;
            _exteriorRequestedVisible = true;
            _scanRequestedVisible = true;
            _cutawayMode = false;
            _explodedAmount = 0f;
            ApplyAllStates();

            if (perspectiveCamera != null && topDownCamera != null)
            {
                perspectiveCamera.enabled = true;
                topDownCamera.enabled = false;
            }

            var orbit = perspectiveCamera != null ? perspectiveCamera.GetComponent<CameraOrbitController>() : null;
            if (orbit != null)
            {
                orbit.ResetView();
            }
        }

        public void UseTopDownCamera(bool enabled)
        {
            if (perspectiveCamera != null) perspectiveCamera.enabled = !enabled;
            if (topDownCamera != null) topDownCamera.enabled = enabled;
            if (!enabled)
            {
                var orbit = perspectiveCamera != null ? perspectiveCamera.GetComponent<CameraOrbitController>() : null;
                if (orbit != null) orbit.SetControlMode(CameraOrbitController.ControlMode.Orbit);
            }
        }

        public void UseFirstPersonWalkMode(bool enabled)
        {
            if (perspectiveCamera != null) perspectiveCamera.enabled = true;
            if (topDownCamera != null) topDownCamera.enabled = false;

            var orbit = perspectiveCamera != null ? perspectiveCamera.GetComponent<CameraOrbitController>() : null;
            if (orbit == null) return;
            orbit.SetControlMode(enabled ? CameraOrbitController.ControlMode.FirstPersonWalk : CameraOrbitController.ControlMode.Orbit);
        }

        // String wrappers for WebGL SendMessage calls from the standalone
        // house-build HTML shell.
        public void SetRoofVisibleWeb(string value) => SetRoofVisible(ParseBool(value, _roofRequestedVisible));

        public void SetSecondFloorVisibleWeb(string value) => SetSecondFloorVisible(ParseBool(value, _secondFloorRequestedVisible));

        public void SetInteriorWallsVisibleWeb(string value) => SetInteriorWallsVisible(ParseBool(value, _interiorRequestedVisible));

        public void SetExteriorShellVisibleWeb(string value) => SetExteriorShellVisible(ParseBool(value, _exteriorRequestedVisible));

        public void SetCutawayModeWeb(string value) => SetCutawayMode(ParseBool(value, _cutawayMode));

        public void SetScanVisibleWeb(string value) => SetScanVisible(ParseBool(value, _scanRequestedVisible));

        public void SetExplodedViewWeb(string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float amount))
            {
                SetExplodedView(amount);
            }
        }

        public void SetCameraModeWeb(string value)
        {
            string mode = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (mode == "top" || mode == "plan")
            {
                UseTopDownCamera(true);
            }
            else if (mode == "walk" || mode == "first-person")
            {
                UseFirstPersonWalkMode(true);
            }
            else
            {
                UseTopDownCamera(false);
            }
        }

        void ApplyAllStates()
        {
            SetActive(secondFloorRoot, _secondFloorRequestedVisible);
            SetActive(interiorWallGroups, _interiorRequestedVisible);
            SetActive(exteriorShellGroups, _exteriorRequestedVisible);
            SetActive(scanGroups, _scanRequestedVisible);
            SetActive(proceduralFinishesRoot, !_scanRequestedVisible);
            ApplyRoofVisibility();
            ApplyCutawayVisibility();
            ApplyExplodedPositions();
        }

        void ApplyRoofVisibility()
        {
            bool visible = _roofRequestedVisible && _exteriorRequestedVisible && !_cutawayMode;
            SetActive(roofRoot, visible);
        }

        void ApplyCutawayVisibility()
        {
            bool visible = _exteriorRequestedVisible && !_cutawayMode;
            SetActive(cutawayWallGroups, visible);
        }

        void ApplyExplodedPositions()
        {
            CaptureBaseTransforms();
            if (roofRoot != null)
            {
                float roofLift = _cutawayMode ? 2f : _explodedAmount * 8f;
                roofRoot.transform.localPosition = _roofBasePosition + Vector3.up * roofLift;
            }

            if (secondFloorRoot != null)
            {
                secondFloorRoot.transform.localPosition = _secondFloorBasePosition + Vector3.up * (_explodedAmount * 4f);
            }
        }

        void CaptureBaseTransforms()
        {
            if (_baseTransformsCaptured) return;
            ResolveMissingReferences();
            if (roofRoot != null) _roofBasePosition = roofRoot.transform.localPosition;
            if (secondFloorRoot != null) _secondFloorBasePosition = secondFloorRoot.transform.localPosition;
            _baseTransformsCaptured = true;
        }

        void ResolveMissingReferences()
        {
            if (roofRoot == null) roofRoot = FindDeep(transform, "Roofs")?.gameObject;
            if (secondFloorRoot == null) secondFloorRoot = FindDeep(transform, "SecondFloor")?.gameObject;
            if (firstFloorRoot == null) firstFloorRoot = FindDeep(transform, "FirstFloor")?.gameObject;

            if (interiorWallGroups == null || interiorWallGroups.Length == 0)
            {
                var interiors = new List<GameObject>();
                AddIfFound(interiors, "FirstFloor/InteriorReferenceWalls");
                AddIfFound(interiors, "SecondFloor/InteriorReferenceWalls");
                AddIfFound(interiors, "Details/StairsApproximation");
                interiorWallGroups = interiors.ToArray();
            }

            if (exteriorShellGroups == null || exteriorShellGroups.Length == 0)
            {
                var exterior = new List<GameObject>();
                AddIfFound(exterior, "FirstFloor/ExteriorWalls");
                AddIfFound(exterior, "FirstFloor/Garage");
                AddIfFound(exterior, "FirstFloor/Doors");
                AddIfFound(exterior, "FirstFloor/Windows");
                AddIfFound(exterior, "SecondFloor/ExteriorWalls");
                AddIfFound(exterior, "SecondFloor/Windows");
                AddIfFound(exterior, "Roofs");
                AddIfFound(exterior, "Details/Trim");
                AddIfFound(exterior, "Details/Gutters");
                AddIfFound(exterior, "Details/Railings");
                exteriorShellGroups = exterior.ToArray();
            }

            if (cutawayWallGroups == null || cutawayWallGroups.Length == 0)
            {
                var cutaways = new List<GameObject>();
                CollectByNameContains(transform, "CutawayWall", cutaways);
                cutawayWallGroups = cutaways.ToArray();
            }

            if (scanGroups == null || scanGroups.Length == 0)
            {
                var scans = new List<GameObject>();
                CollectByNameContains(transform, "AsBuilt", scans);
                scanGroups = scans.ToArray();
            }

            if (proceduralFinishesRoot == null)
            {
                proceduralFinishesRoot = FindDeep(transform, "ProceduralFinishes")?.gameObject;
            }

            if (perspectiveCamera == null)
            {
                var camTransform = FindDeep(transform, "MainPerspectiveCamera");
                if (camTransform != null) perspectiveCamera = camTransform.GetComponent<Camera>();
            }

            if (topDownCamera == null)
            {
                var camTransform = FindDeep(transform, "TopDownPlanCamera");
                if (camTransform != null) topDownCamera = camTransform.GetComponent<Camera>();
            }
        }

        void AddIfFound(List<GameObject> list, string path)
        {
            var t = transform.Find(path);
            if (t != null) list.Add(t.gameObject);
        }

        static void SetActive(GameObject target, bool visible)
        {
            if (target != null) target.SetActive(visible);
        }

        static void SetActive(IEnumerable<GameObject> targets, bool visible)
        {
            if (targets == null) return;
            foreach (var target in targets)
            {
                if (target != null) target.SetActive(visible);
            }
        }

        static bool ParseBool(string value, bool fallback)
        {
            if (bool.TryParse(value, out bool result)) return result;
            string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "1" || normalized == "yes" || normalized == "on") return true;
            if (normalized == "0" || normalized == "no" || normalized == "off") return false;
            return fallback;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindDeep(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        static void CollectByNameContains(Transform root, string token, List<GameObject> result)
        {
            if (root.name.Contains(token)) result.Add(root.gameObject);
            for (int i = 0; i < root.childCount; i++)
            {
                CollectByNameContains(root.GetChild(i), token, result);
            }
        }
    }
}
