#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CommerceDemo.EditorTools
{
    /// <summary>
    /// Builds the standalone scanned-plan house scene for local browser review.
    /// It leaves the commerce demo's WebGL build path untouched.
    /// </summary>
    public static class HouseWebGLBuilder
    {
        const string ScenePath = "Assets/Scenes/HouseModel_ScannedPlan.unity";
        const string DefaultOutput = "../web-shell/house-build";

        [MenuItem("Tools/House Model/Build WebGL House Preview")]
        public static void BuildFromMenu()
        {
            Build(DefaultOutput);
        }

        public static void BuildCommandLine()
        {
            string output = DefaultOutput;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildOutput") output = args[i + 1];
            }
            Build(output);
        }

        static void Build(string relativeOutput)
        {
            EnsureHouseScene();
            EnsureStandardShaderAlwaysIncluded();

            PlayerSettings.companyName = "CommerceDemo";
            PlayerSettings.productName = "Scanned Plan House Preview";
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.template = "PROJECT:CommerceShell";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.stripEngineCode = false;

            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeOutput));
            Directory.CreateDirectory(output);

            var report = BuildPipeline.BuildPlayer(
                new[] { ScenePath }, output, BuildTarget.WebGL, BuildOptions.None);

            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[HouseWebGLBuilder] Build succeeded -> {output} ({summary.totalSize / (1024f * 1024f):0.0} MB)");
            }
            else
            {
                Debug.LogError($"[HouseWebGLBuilder] Build {summary.result} with {summary.totalErrors} errors.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        static void EnsureHouseScene()
        {
            if (File.Exists(ScenePath)) return;

            HouseModelAssetBuilder.CreateAssets();
            if (File.Exists(ScenePath)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("HouseModel_ScannedPlan");
            root.AddComponent<HouseModelGenerator>().BuildHouse();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
        }

        static void EnsureStandardShaderAlwaysIncluded()
        {
            var shader = Shader.Find("Standard");
            if (shader == null) return;

            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (assets == null || assets.Length == 0) return;

            var serialized = new SerializedObject(assets[0]);
            var list = serialized.FindProperty("m_AlwaysIncludedShaders");
            if (list == null) return;

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;
            }

            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            list.GetArrayElementAtIndex(index).objectReferenceValue = shader;
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
