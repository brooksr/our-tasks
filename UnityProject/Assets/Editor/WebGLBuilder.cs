#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CommerceDemo.EditorTools
{
    /// <summary>
    /// One-click / one-command WebGL builds.
    ///
    /// Editor:  Tools → Commerce Demo → Build WebGL (to web-shell)
    /// CLI:     Unity -batchmode -quit -projectPath UnityProject \
    ///            -executeMethod CommerceDemo.EditorTools.WebGLBuilder.BuildCommandLine \
    ///            [-buildOutput ../web-shell/unity-build] -logFile build.log
    ///
    /// The default output (web-shell/unity-build) is where the web shell looks
    /// for the build. Unity names build files after the output folder, so the
    /// shell loads unity-build/Build/unity-build.loader.js etc.
    /// </summary>
    public static class WebGLBuilder
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string DefaultOutput = "../web-shell/unity-build";

        [MenuItem("Tools/Commerce Demo/Create Main Scene")]
        public static void CreateMainScene()
        {
            EnsureMainScene();
        }

        [MenuItem("Tools/Commerce Demo/Build WebGL (to web-shell)")]
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
            EnsureMainScene();
            EnsureStandardShaderAlwaysIncluded();

            PlayerSettings.companyName = "CommerceDemo";
            PlayerSettings.productName = "Unity Commerce Demo";
            PlayerSettings.runInBackground = true;
            PlayerSettings.WebGL.template = "PROJECT:CommerceShell";
            // Uncompressed output avoids web-server Content-Encoding
            // configuration during prototyping. For production, switch to
            // Brotli + decompression fallback (see docs/build-webgl.md).
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            // The demo is entirely runtime-built. In Unity 6 WebGL, engine
            // stripping can spam the browser console for generated script
            // metadata even when the scene runs correctly.
            PlayerSettings.stripEngineCode = false;

            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativeOutput));
            Directory.CreateDirectory(output);

            var report = BuildPipeline.BuildPlayer(
                new[] { ScenePath }, output, BuildTarget.WebGL, BuildOptions.None);

            var summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[WebGLBuilder] Build succeeded → {output} ({summary.totalSize / (1024f * 1024f):0.0} MB)");
            }
            else
            {
                Debug.LogError($"[WebGLBuilder] Build {summary.result} with {summary.totalErrors} errors.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// The demo builds its whole object graph at runtime (AppBootstrap),
        /// so the scene only has to exist — an empty one is created on demand.
        /// </summary>
        static void EnsureMainScene()
        {
            if (File.Exists(ScenePath))
            {
                Debug.Log($"[WebGLBuilder] Scene already exists at {ScenePath}.");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"[WebGLBuilder] Created empty scene at {ScenePath}.");
        }

        /// <summary>
        /// All materials are created from Shader.Find("Standard") at runtime.
        /// Since no scene/asset references the Standard shader, Unity would
        /// strip it from the build — force-include it in Graphics Settings.
        /// </summary>
        static void EnsureStandardShaderAlwaysIncluded()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogWarning("[WebGLBuilder] Standard shader not found; skipping always-include step.");
                return;
            }

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
            Debug.Log("[WebGLBuilder] Added Standard shader to Always Included Shaders.");
        }
    }
}
#endif
