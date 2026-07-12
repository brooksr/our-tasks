using System;
using System.Collections.Generic;

namespace CommerceDemo
{
    // Serializable data models mirroring the JSON schemas in StreamingAssets.
    // All parsing uses Unity's built-in JsonUtility, so fields are public and
    // classes are marked [Serializable]. Keep these generic — nothing in here
    // should be furniture- or retailer-specific beyond the sample data itself.

    [Serializable]
    public class ThemeConfig
    {
        public string primaryColor;
        public string accentColor;
        public string backgroundColor;
        public string surfaceColor;
        public string textColor;
    }

    [Serializable]
    public class ScenePreset
    {
        public string roomPreset = "standard";
        public string wallColor = "#EFE7DC";
        public string floorColor = "#B08D6A";
        public float ambientIntensity = 1f;
    }

    [Serializable]
    public class RetailerConfig
    {
        public string retailerName;
        public string demoLabel;
        public string industry;
        public string currency = "USD";
        public string currencySymbol = "$";
        public ThemeConfig theme;
        public List<string> nav;
        public string mainProductId;
        public string recommendationStrategyName;
        public ScenePreset scenePreset;
    }

    [Serializable]
    public class ProductVariant
    {
        public string id;
        public string name;
        public string color;
    }

    [Serializable]
    public class Product
    {
        public string id;
        public string name;
        public float price;
        public float rating;
        public int reviews;
        public string category;
        public string description;
        public string sceneObjectType;
        public List<ProductVariant> variants;
    }

    [Serializable]
    public class ProductCatalog
    {
        public List<Product> products;
    }

    [Serializable]
    public class RecommendedProduct
    {
        public string id;
        public string name;
        public string category;
        public float price;
        public string reason;
        public bool selectedDefault;
        public string color;
        public string material;
        public int bundlePriority;
        public string sceneObjectType;
    }

    [Serializable]
    public class RecommendationSet
    {
        public string strategyName;
        public string mainProductId;
        public List<RecommendedProduct> items;
    }
}
