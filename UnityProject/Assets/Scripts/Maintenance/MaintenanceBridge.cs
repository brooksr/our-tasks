using System;
using System.Collections.Generic;
using UnityEngine;

namespace OurTasks.Maintenance
{
    public sealed class MaintenanceBridge : MonoBehaviour
    {
        readonly Dictionary<string, HomeAsset> assets = new();
        readonly Dictionary<Renderer, Color> originalColors = new();
        bool maintenanceMode;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<MaintenanceBridge>() != null) return;
            new GameObject("MaintenanceBridge").AddComponent<MaintenanceBridge>();
        }

        void Awake()
        {
            gameObject.name = "MaintenanceBridge";
            RebuildIndex();
        }

        public void RebuildIndex()
        {
            assets.Clear();
            foreach (var asset in FindObjectsByType<HomeAsset>(FindObjectsInactive.Include))
            {
                Index(asset.assetId, asset);
                Index(asset.unityObjectId, asset);
                Index(asset.StableId, asset);
            }
        }

        void Index(string key, HomeAsset asset)
        {
            if (!string.IsNullOrWhiteSpace(key) && !assets.ContainsKey(key)) assets.Add(key, asset);
        }

        public void EnterMaintenanceMode()
        {
            maintenanceMode = true;
            foreach (var asset in assets.Values) ApplyState(asset, "neutral");
        }

        public void ExitMaintenanceMode()
        {
            maintenanceMode = false;
            foreach (var pair in originalColors)
            {
                if (pair.Key == null) continue;
                var block = new MaterialPropertyBlock();
                pair.Key.GetPropertyBlock(block);
                block.SetColor("_Color", pair.Value);
                block.SetColor("_EmissionColor", Color.black);
                pair.Key.SetPropertyBlock(block);
            }
            originalColors.Clear();
        }

        public void HighlightAsset(string assetId)
        {
            if (assets.TryGetValue(assetId, out var asset)) ApplyState(asset, "today");
        }

        public void HighlightRoom(string roomId)
        {
            foreach (var asset in assets.Values) if (asset.roomId == roomId) ApplyState(asset, "today");
        }

        public void RefreshStatuses(string json)
        {
            if (!maintenanceMode) EnterMaintenanceMode();
            StatusItem[] items;
            try { items = JsonUtility.FromJson<StatusEnvelope>("{\"items\":" + json + "}").items ?? Array.Empty<StatusItem>(); }
            catch (Exception exception) { Debug.LogWarning($"[Maintenance] Invalid status payload: {exception.Message}"); return; }
            foreach (var item in items)
            {
                if (!string.IsNullOrWhiteSpace(item.assetId) && assets.TryGetValue(item.assetId, out var asset)) ApplyState(asset, item.state);
                if (item.unityObjectIds == null) continue;
                foreach (var objectId in item.unityObjectIds) if (assets.TryGetValue(objectId, out var mapped)) ApplyState(mapped, item.state);
            }
        }

        void ApplyState(HomeAsset asset, string state)
        {
            var color = state switch
            {
                "overdue" => new Color(0.86f, 0.18f, 0.12f),
                "today" => new Color(0.95f, 0.58f, 0.1f),
                "soon" => new Color(0.9f, 0.75f, 0.25f),
                "completed" => new Color(0.2f, 0.65f, 0.4f),
                _ => new Color(0.38f, 0.42f, 0.4f)
            };
            foreach (var renderer in asset.GetComponentsInChildren<Renderer>(true))
            {
                if (!originalColors.ContainsKey(renderer)) originalColors[renderer] = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color") ? renderer.sharedMaterial.color : Color.white;
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", Color.Lerp(originalColors[renderer], color, state == "neutral" ? 0.62f : 0.3f));
                block.SetColor("_EmissionColor", state == "neutral" ? Color.black : color * 0.22f);
                renderer.SetPropertyBlock(block);
            }
            asset.gameObject.name = asset.gameObject.name.Split(new[] { " [" }, StringSplitOptions.None)[0] + $" [{state}]";
        }

        [Serializable] sealed class StatusEnvelope { public StatusItem[] items; }
        [Serializable] sealed class StatusItem { public string assetId; public string roomId; public string[] unityObjectIds; public string state; }
    }
}
