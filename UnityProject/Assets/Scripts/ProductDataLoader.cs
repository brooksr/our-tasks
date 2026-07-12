using System;
using System.Collections;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Loads and parses the product catalog and the recommendation set.
    /// Both can come from StreamingAssets (standalone testing) or be pushed
    /// in as JSON strings from the host web page (embedded testing).
    /// </summary>
    public static class ProductDataLoader
    {
        public const string ProductsFileName = "products.json";
        public const string RecommendationsFileName = "recommendations.json";

        public static IEnumerator LoadProducts(Action<ProductCatalog> onLoaded, Action<string> onError = null)
        {
            yield return StreamingAssetsJson.Load(
                ProductsFileName,
                json => onLoaded?.Invoke(ParseProducts(json)),
                onError);
        }

        public static IEnumerator LoadRecommendations(Action<RecommendationSet> onLoaded, Action<string> onError = null)
        {
            yield return StreamingAssetsJson.Load(
                RecommendationsFileName,
                json => onLoaded?.Invoke(ParseRecommendations(json)),
                onError);
        }

        public static ProductCatalog ParseProducts(string json)
        {
            try
            {
                return JsonUtility.FromJson<ProductCatalog>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProductDataLoader] Failed to parse products: {e.Message}");
                return null;
            }
        }

        public static RecommendationSet ParseRecommendations(string json)
        {
            try
            {
                return JsonUtility.FromJson<RecommendationSet>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProductDataLoader] Failed to parse recommendations: {e.Message}");
                return null;
            }
        }
    }
}
