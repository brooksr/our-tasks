#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CommerceDemo.EditorTools
{
    /// <summary>
    /// Bakes the procedural scanned-plan house into a prefab and standalone
    /// scene for inspection. The generator remains the source of truth; rerun
    /// this utility after dimension or material changes.
    /// </summary>
    public static class HouseModelAssetBuilder
    {
        const string PrefabPath = "Assets/Prefabs/HouseModel_ScannedPlan.prefab";
        const string ScenePath = "Assets/Scenes/HouseModel_ScannedPlan.unity";
        const string MaterialFolder = "Assets/Materials/HouseMaterials";

        [MenuItem("Tools/House Model/Create Scanned Plan Scene and Prefab")]
        public static void CreateAssets()
        {
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory(MaterialFolder);

            FirstFloorScanAssetBuilder.CreateAllAssets();
            var materials = CreateMaterials();
            var scanMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FirstFloorScanAssetBuilder.MeshPath);
            var scanMaterial = AssetDatabase.LoadAssetAtPath<Material>(FirstFloorScanAssetBuilder.MaterialPath);
            var garageMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FirstFloorScanAssetBuilder.GarageMeshPath);
            var garageMaterial = AssetDatabase.LoadAssetAtPath<Material>(FirstFloorScanAssetBuilder.GarageMaterialPath);
            var upstairsMesh = AssetDatabase.LoadAssetAtPath<Mesh>(FirstFloorScanAssetBuilder.UpstairsMeshPath);
            var upstairsMaterial = AssetDatabase.LoadAssetAtPath<Material>(FirstFloorScanAssetBuilder.UpstairsMaterialPath);
            var root = new GameObject("HouseModel_ScannedPlan");
            var generator = root.AddComponent<HouseModelGenerator>();
            root.AddComponent<HouseModelController>();

            generator.generateInEditMode = false;
            generator.generateOnAwake = true;
            generator.clearExisting = true;
            generator.SetMaterialOverrides(
                materials.stucco,
                materials.stuccoAccent,
                materials.roof,
                materials.fascia,
                materials.trim,
                materials.garageDoor,
                materials.glass,
                materials.door,
                materials.concrete,
                materials.ground,
                materials.driveway,
                materials.interiorWall,
                materials.railing,
                materials.darkTrim,
                materials.deck);
            generator.SetScannedFirstFloorOverride(scanMesh, scanMaterial);
            generator.SetScannedGarageOverride(garageMesh, garageMaterial);
            generator.SetScannedUpstairsOverride(upstairsMesh, upstairsMaterial);
            generator.BuildHouse();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var sceneRoot = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (sceneRoot != null)
            {
                sceneRoot.name = "HouseModel_ScannedPlan";
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[HouseModelAssetBuilder] Created {PrefabPath} and {ScenePath}.");
        }

        public static void CreateCommandLine()
        {
            CreateAssets();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        static HouseMaterials CreateMaterials()
        {
            var stuccoTexture = HouseModelGenerator.CreateEditorTextureAsset($"{MaterialFolder}/StuccoNoise.asset", shingles: false);
            var shingleTexture = HouseModelGenerator.CreateEditorTextureAsset($"{MaterialFolder}/CompositionShingleLines.asset", shingles: true);

            // Palette matched to the private home reference photos.
            return new HouseMaterials
            {
                stucco = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Stucco_Charcoal.mat", new Color(0.29f, 0.29f, 0.30f), 0.18f, false, stuccoTexture),
                stuccoAccent = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Stucco_CharcoalLight.mat", new Color(0.34f, 0.34f, 0.35f), 0.18f, false, stuccoTexture),
                roof = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Roof_CompositionShingle.mat", new Color(0.38f, 0.37f, 0.36f), 0.12f, false, shingleTexture),
                fascia = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Fascia_White.mat", new Color(0.92f, 0.92f, 0.90f), 0.2f, false),
                trim = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Trim_White.mat", new Color(0.94f, 0.94f, 0.92f), 0.18f, false),
                garageDoor = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/GarageDoor_White.mat", new Color(0.93f, 0.93f, 0.91f), 0.22f, false),
                glass = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Glass_DarkBlueGray.mat", new Color(0.58f, 0.66f, 0.70f, 0.22f), 0.85f, true),
                door = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Door_White.mat", new Color(0.90f, 0.90f, 0.88f), 0.25f, false),
                concrete = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Concrete_LightGray.mat", new Color(0.62f, 0.62f, 0.59f), 0.1f, false),
                ground = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Ground_Earth.mat", new Color(0.52f, 0.45f, 0.33f), 0.25f, false),
                driveway = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Driveway_Pavers.mat", new Color(0.66f, 0.62f, 0.57f), 0.08f, false),
                interiorWall = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/InteriorReference_BlueGray.mat", new Color(0.62f, 0.69f, 0.72f), 0.2f, false),
                railing = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Railing_White.mat", new Color(0.92f, 0.92f, 0.90f), 0.18f, false),
                darkTrim = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/DarkTrim_Gutter.mat", new Color(0.18f, 0.17f, 0.15f), 0.15f, false),
                deck = HouseModelGenerator.CreateEditorMaterialAsset($"{MaterialFolder}/Deck_Redwood.mat", new Color(0.42f, 0.27f, 0.17f), 0.2f, false)
            };
        }

        struct HouseMaterials
        {
            public Material stucco;
            public Material stuccoAccent;
            public Material roof;
            public Material fascia;
            public Material trim;
            public Material garageDoor;
            public Material glass;
            public Material door;
            public Material concrete;
            public Material ground;
            public Material driveway;
            public Material interiorWall;
            public Material railing;
            public Material darkTrim;
            public Material deck;
        }
    }
}
#endif
