using System.Collections;
using System.Linq;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Central orchestrator. On startup it loads the three JSON files from
    /// StreamingAssets (so the scene runs standalone in the editor or a raw
    /// WebGL build), builds the room, and announces itself to the host page.
    /// The host page can also push replacement JSON at any time through
    /// WebGLBridge, which routes to the Apply*Json methods here — that is what
    /// makes the experience retailer- and industry-agnostic.
    /// </summary>
    public class CommerceAppController : MonoBehaviour
    {
        public RetailerConfig Config { get; private set; }
        public ProductCatalog Catalog { get; private set; }
        public RecommendationSet RecommendationData { get; private set; }
        public Product MainProduct { get; private set; }

        public WebGLBridge Bridge { get; private set; }
        public RoomSceneController Room { get; private set; }
        public RecommendationManager Recommendations { get; private set; }
        public SofaController Sofa { get; private set; }

        void Awake()
        {
            Bridge = GetComponent<WebGLBridge>();
            Room = GetComponent<RoomSceneController>();
            Recommendations = GetComponent<RecommendationManager>();
            Sofa = GetComponent<SofaController>();
        }

        IEnumerator Start()
        {
            yield return RetailerConfigLoader.Load(config => Config = config);
            yield return ProductDataLoader.LoadProducts(catalog => Catalog = catalog);
            yield return ProductDataLoader.LoadRecommendations(set => RecommendationData = set);
            BuildScene();
        }

        // ---- Data pushed in from the host web page (see WebGLBridge) ----

        public void ApplyRetailerConfigJson(string json)
        {
            var config = RetailerConfigLoader.Parse(json);
            if (config == null) return;
            Config = config;
            BuildScene();
        }

        public void ApplyProductsJson(string json)
        {
            var catalog = ProductDataLoader.ParseProducts(json);
            if (catalog == null) return;
            Catalog = catalog;
            BuildScene();
        }

        public void ApplyRecommendationsJson(string json)
        {
            var set = ProductDataLoader.ParseRecommendations(json);
            if (set == null) return;
            RecommendationData = set;
            BuildScene();
        }

        // ---- Scene lifecycle ----

        public void BuildScene()
        {
            // Wait until all three data sources are present before building.
            if (Config == null || Catalog == null || RecommendationData == null) return;

            MainProduct = Catalog.products?.FirstOrDefault(p => p.id == Config.mainProductId)
                          ?? Catalog.products?.FirstOrDefault();
            if (MainProduct == null)
            {
                Debug.LogError("[CommerceApp] No products available — cannot build scene.");
                return;
            }

            ApplyEnvironment();
            Room.BuildRoom(Config, MainProduct, RecommendationData);
            Recommendations.Initialize(RecommendationData, MainProduct);

            Bridge.Emit(AnalyticsEvent.SceneLoaded, AnalyticsEvent.Payload(
                ("retailerName", Config.retailerName),
                ("demoLabel", Config.demoLabel),
                ("industry", Config.industry),
                ("recommendationStrategy", Config.recommendationStrategyName),
                ("roomSize", Room.CurrentRoomSize),
                ("mainProductId", MainProduct.id),
                ("bundleTotal", Recommendations.BundleTotal)));

            Bridge.Emit(AnalyticsEvent.ProductViewed, AnalyticsEvent.Payload(
                ("productId", MainProduct.id),
                ("name", MainProduct.name),
                ("price", MainProduct.price),
                ("category", MainProduct.category)));
        }

        void ApplyEnvironment()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.backgroundColor = SceneObjectFactory.ParseColor(
                    Config.theme?.backgroundColor, new Color(0.96f, 0.94f, 0.9f));
            }

            float ambient = Config.scenePreset != null ? Mathf.Clamp(Config.scenePreset.ambientIntensity, 0.2f, 2f) : 1f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.6f, 0.57f) * ambient;
        }

        public void ResetRoom()
        {
            if (Config == null) return;
            Sofa.ResetColor();
            Room.SetRoomSize(Config.scenePreset?.roomPreset ?? "standard", emitEvent: false);
            Recommendations.ResetToDefaults();

            var cam = Camera.main;
            var orbit = cam != null ? cam.GetComponent<CameraOrbitController>() : null;
            if (orbit != null) orbit.ResetView();
        }

        /// <summary>Emits the full current configuration under the given event name.</summary>
        public void EmitSelectedConfiguration(string eventName)
        {
            Bridge.Emit(eventName, AnalyticsEvent.Payload(
                ("retailerName", Config?.retailerName),
                ("mainProductId", MainProduct?.id),
                ("sofaColor", Sofa.CurrentColorHex),
                ("roomSize", Room.CurrentRoomSize),
                ("selectedRecommendationIds", AnalyticsEvent.Raw(AnalyticsEvent.JsonStringArray(Recommendations.SelectedIds))),
                ("selectedCount", Recommendations.SelectedIds.Count),
                ("bundleTotal", Recommendations.BundleTotal),
                ("currency", Config?.currency)));
        }
    }
}
