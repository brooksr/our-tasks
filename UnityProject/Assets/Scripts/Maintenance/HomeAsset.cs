using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OurTasks.Maintenance
{
    [DisallowMultipleComponent]
    public sealed class HomeAsset : MonoBehaviour
    {
        [SerializeField] string stableId;
        public string assetId;
        public string displayName;
        public string roomId;
        public string category;
        public string unityObjectId;
        public string smartDeviceId;

        public string StableId => stableId;

        void Reset()
        {
            EnsureStableId();
            if (string.IsNullOrWhiteSpace(displayName)) displayName = gameObject.name;
            if (string.IsNullOrWhiteSpace(unityObjectId)) unityObjectId = gameObject.name;
        }

        void OnValidate() => EnsureStableId();

        void EnsureStableId()
        {
            if (string.IsNullOrWhiteSpace(stableId)) stableId = Guid.NewGuid().ToString("N");
        }

        void OnMouseDown()
        {
            var payload = JsonUtility.ToJson(new AssetSelection
            {
                assetId = assetId,
                roomId = roomId,
                unityObjectId = unityObjectId
            });
            NotifyWeb(payload);
        }

        static void NotifyWeb(string payload)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            MaintenanceAssetSelected(payload);
#else
            Debug.Log($"[Maintenance] Selected {payload}");
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void MaintenanceAssetSelected(string payload);
#endif

        [Serializable]
        struct AssetSelection
        {
            public string assetId;
            public string roomId;
            public string unityObjectId;
        }
    }
}
