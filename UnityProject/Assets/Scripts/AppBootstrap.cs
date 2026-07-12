using UnityEngine;
using UnityEngine.Rendering;

namespace CommerceDemo
{
    /// <summary>
    /// Builds the entire runtime object graph from code so the demo needs no
    /// hand-authored scene content — any empty scene works. This keeps the
    /// repository free of binary/YAML scene wiring and makes the setup fully
    /// data-driven: camera, lighting, and the "CommerceApp" GameObject (the
    /// SendMessage target for the web shell) are all created here.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            if (Object.FindObjectOfType<CommerceAppController>() != null) return;
            if (Object.FindObjectOfType<HouseModelGenerator>() != null) return;

            // The GameObject name "CommerceApp" is the SendMessage target used
            // by web-shell/unity-bridge.js — keep them in sync.
            var app = new GameObject("CommerceApp");
            app.AddComponent<RoomSceneController>();
            app.AddComponent<RecommendationManager>();
            app.AddComponent<SofaController>();
            app.AddComponent<WebGLBridge>();
            app.AddComponent<CommerceAppController>();

            if (Camera.main == null)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.96f, 0.94f, 0.9f);
                cam.fieldOfView = 50f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 60f;
                camGo.AddComponent<CameraOrbitController>();
            }
            else if (Camera.main.GetComponent<CameraOrbitController>() == null)
            {
                Camera.main.gameObject.AddComponent<CameraOrbitController>();
            }

            // One warm directional key light with soft shadows — cheap enough
            // for WebGL while still grounding the furniture.
            var lightGo = new GameObject("Key Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 0.95f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.55f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.6f, 0.57f);
        }
    }
}
