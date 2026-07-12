using System;
using System.Collections;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Loads and parses the retailer configuration. The config drives theming,
    /// navigation, the main product selection, and the scene preset — swapping
    /// this one file re-skins the whole demo for a different retailer/industry.
    /// </summary>
    public static class RetailerConfigLoader
    {
        public const string FileName = "retailer-config.json";

        public static IEnumerator Load(Action<RetailerConfig> onLoaded, Action<string> onError = null)
        {
            yield return StreamingAssetsJson.Load(
                FileName,
                json => onLoaded?.Invoke(Parse(json)),
                onError);
        }

        public static RetailerConfig Parse(string json)
        {
            try
            {
                var config = JsonUtility.FromJson<RetailerConfig>(json);
                if (config == null || string.IsNullOrEmpty(config.retailerName))
                {
                    Debug.LogWarning("[RetailerConfigLoader] Config parsed but looks empty — check retailer-config.json.");
                }
                return config;
            }
            catch (Exception e)
            {
                Debug.LogError($"[RetailerConfigLoader] Failed to parse retailer config: {e.Message}");
                return null;
            }
        }
    }
}
