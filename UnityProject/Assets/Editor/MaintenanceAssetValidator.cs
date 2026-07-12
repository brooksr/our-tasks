#if UNITY_EDITOR
using System.Collections.Generic;
using OurTasks.Maintenance;
using UnityEditor;
using UnityEngine;

namespace OurTasks.Editor
{
    public static class MaintenanceAssetValidator
    {
        [MenuItem("Tools/Our Tasks/Validate asset mappings")]
        public static void ValidateMappings()
        {
            var seen = new Dictionary<string, HomeAsset>();
            var issues = 0;
            foreach (var asset in Object.FindObjectsByType<HomeAsset>(FindObjectsInactive.Include))
            {
                if (string.IsNullOrWhiteSpace(asset.StableId)) { Debug.LogError($"Missing stable ID on {asset.name}", asset); issues++; }
                else if (seen.TryGetValue(asset.StableId, out var duplicate)) { Debug.LogError($"Duplicate stable ID on {duplicate.name} and {asset.name}", asset); issues++; }
                else seen.Add(asset.StableId, asset);
                if (string.IsNullOrWhiteSpace(asset.assetId) && string.IsNullOrWhiteSpace(asset.roomId)) { Debug.LogWarning($"{asset.name} maps to neither an asset nor a room.", asset); issues++; }
            }
            Debug.Log(issues == 0 ? $"[Our Tasks] {seen.Count} maintenance mappings are valid." : $"[Our Tasks] Validation found {issues} mapping issue(s).");
        }
    }
}
#endif
