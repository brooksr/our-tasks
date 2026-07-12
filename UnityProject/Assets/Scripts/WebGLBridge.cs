using System.Runtime.InteropServices;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Bidirectional bridge between Unity and the host web page.
    ///
    /// Unity → JavaScript: Emit() calls into Plugins/WebGL/CommerceBridge.jslib,
    /// which forwards (eventName, payload) to window.UnityCommerceBridge.handleUnityEvent.
    /// In the editor / non-WebGL players it logs to the console instead.
    ///
    /// JavaScript → Unity: the public methods below are invoked with
    /// unityInstance.SendMessage("CommerceApp", "MethodName", stringArg).
    /// SendMessage only supports zero or one string/number argument, so
    /// structured input (like a toggle) arrives as a small JSON string.
    /// </summary>
    public class WebGLBridge : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void CommerceBridge_Emit(string eventName, string payloadJson);
#endif

        CommerceAppController _app;
        CommerceAppController App => _app != null ? _app : (_app = GetComponent<CommerceAppController>());

        public void Emit(string eventName, string payloadJson)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CommerceBridge_Emit(eventName, payloadJson);
#else
            Debug.Log($"[CommerceBridge] {eventName} {payloadJson}");
#endif
        }

        // ------- Methods invoked from JavaScript via SendMessage -------

        public void LoadRetailerConfig(string json) => App.ApplyRetailerConfigJson(json);

        public void LoadProducts(string json) => App.ApplyProductsJson(json);

        public void LoadRecommendations(string json) => App.ApplyRecommendationsJson(json);

        /// <summary>Expects {"productId":"...","selected":true|false}.</summary>
        public void ToggleRecommendation(string json)
        {
            var message = JsonUtility.FromJson<ToggleMessage>(json);
            if (message == null || string.IsNullOrEmpty(message.productId))
            {
                Debug.LogWarning($"[CommerceBridge] Bad ToggleRecommendation payload: {json}");
                return;
            }
            App.Recommendations.SetSelected(message.productId, message.selected, emitEvent: true);
        }

        public void ChangeSofaColor(string hexColor) => App.Sofa.ApplyColor(hexColor);

        public void ChangeRoomSize(string size) => App.Room.SetRoomSize(size);

        public void ResetRoom() => App.ResetRoom();

        public void RequestSelectedConfiguration() => App.EmitSelectedConfiguration("selected_configuration");

        public void SaveConfiguration() => App.EmitSelectedConfiguration(AnalyticsEvent.ConfigurationSaved);

        public void AddBundleToCart() => App.EmitSelectedConfiguration(AnalyticsEvent.BundleAdded);

        [System.Serializable]
        class ToggleMessage
        {
            public string productId;
            public bool selected;
        }
    }
}
