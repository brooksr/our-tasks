#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CommerceDemo.EditorTools
{
    /// <summary>
    /// Renders the generated house model to PNGs from reference viewpoints so
    /// the model can be compared against the private home reference photos
    /// without a WebGL rebuild. Batch usage:
    ///   -executeMethod CommerceDemo.EditorTools.HouseSnapshotTool.CaptureCommandLine -snapshotDir /path
    /// </summary>
    public static class HouseSnapshotTool
    {
        public static void CaptureCommandLine()
        {
            string dir = "house-snapshots";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-snapshotDir") dir = args[i + 1];
            }
            Capture(dir);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        [MenuItem("Tools/House Model/Capture Reference Snapshots")]
        public static void CaptureFromMenu()
        {
            Capture("house-snapshots");
        }

        static void Capture(string dir)
        {
            Directory.CreateDirectory(dir);

            var root = new GameObject("SnapshotHouse");
            var generator = root.AddComponent<HouseModelGenerator>();
            generator.generateOnAwake = false;
            generator.generateInEditMode = false;
            generator.includeCamerasAndLighting = false;
            FirstFloorScanAssetBuilder.CreateAllAssets();
            generator.SetScannedFirstFloorOverride(
                AssetDatabase.LoadAssetAtPath<Mesh>(FirstFloorScanAssetBuilder.MeshPath),
                AssetDatabase.LoadAssetAtPath<Material>(FirstFloorScanAssetBuilder.MaterialPath));
            generator.SetScannedGarageOverride(
                AssetDatabase.LoadAssetAtPath<Mesh>(FirstFloorScanAssetBuilder.GarageMeshPath),
                AssetDatabase.LoadAssetAtPath<Material>(FirstFloorScanAssetBuilder.GarageMaterialPath));
            generator.SetScannedUpstairsOverride(
                AssetDatabase.LoadAssetAtPath<Mesh>(FirstFloorScanAssetBuilder.UpstairsMeshPath),
                AssetDatabase.LoadAssetAtPath<Material>(FirstFloorScanAssetBuilder.UpstairsMaterialPath));
            generator.BuildHouse();

            var lightGo = new GameObject("SnapshotSun");
            var sun = lightGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.86f);
            sun.intensity = 1.05f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.58f, 0.58f, 0.55f);

            var camGo = new GameObject("SnapshotCamera");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.74f, 0.79f, 0.82f);
            cam.fieldOfView = 42f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;

            Vector3 target = new Vector3(0f, 6f, 2f);
            Shot(cam, dir, "front_street", target, new Vector3(-14f, 9f, -78f));
            Shot(cam, dir, "front_right", target, new Vector3(42f, 12f, -62f));
            Shot(cam, dir, "rear_deck", new Vector3(0f, 2f, 18f), new Vector3(14f, -2f, 78f));
            Shot(cam, dir, "left_side", target, new Vector3(-70f, 10f, 8f));
            Shot(cam, dir, "right_side", target, new Vector3(70f, 10f, 8f));
            Shot(cam, dir, "aerial", new Vector3(0f, 0f, 4f), new Vector3(-34f, 60f, -52f));

            // Interior verification views. These are especially useful after
            // regenerating the prefab because the exterior reference shots do
            // not reveal the room partitions or the stair geometry.
            SetActive(root, "Roofs", false);
            SetActive(root, "Site", false);
            SetActive(root, "SecondFloor", false);
            PlanShot(cam, dir, "ground_floor_plan", new Vector3(0f, 0f, 1f), new Vector3(0f, 62f, 1f), 29f);

            SetActive(root, "FirstFloor", false);
            SetActive(root, "SecondFloor", true);
            PlanShot(cam, dir, "upper_floor_plan", new Vector3(0f, 8f, 11f), new Vector3(0f, 62f, 11f), 18f);

            SetActive(root, "FirstFloor", true);
            SetActive(root, "SecondFloor", false);
            SetActive(root, "FirstFloor/ExteriorWalls", false);
            SetActive(root, "FirstFloor/Garage", false);
            SetActive(root, "FirstFloor/FrontPorch", false);
            SetActive(root, "FirstFloor/RearPorch", false);
            SetActive(root, "FirstFloor/Doors", false);
            SetActive(root, "FirstFloor/Windows", false);
            Shot(cam, dir, "entry_stair_cutaway", new Vector3(-9.8f, 3.8f, -3.1f), new Vector3(-25f, 11f, -18f));

            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Object.DestroyImmediate(root);
            Debug.Log($"[HouseSnapshotTool] Wrote snapshots to {Path.GetFullPath(dir)}");
        }

        static void Shot(Camera cam, string dir, string name, Vector3 target, Vector3 position)
        {
            cam.orthographic = false;
            cam.transform.position = position;
            cam.transform.LookAt(target);

            Render(cam, dir, name);
        }

        static void PlanShot(Camera cam, string dir, string name, Vector3 target, Vector3 position, float size)
        {
            cam.orthographic = true;
            cam.orthographicSize = size;
            cam.transform.position = position;
            cam.transform.LookAt(target);

            Render(cam, dir, name);
        }

        static void Render(Camera cam, string dir, string name)
        {

            const int w = 1280, h = 800;
            var rt = new RenderTexture(w, h, 24);
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;

            File.WriteAllBytes(Path.Combine(dir, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }

        static void SetActive(GameObject root, string path, bool active)
        {
            var child = root.transform.Find(path);
            if (child != null) child.gameObject.SetActive(active);
        }
    }
}
#endif
