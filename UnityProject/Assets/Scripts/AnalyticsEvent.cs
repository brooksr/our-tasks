using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CommerceDemo
{
    /// <summary>
    /// Canonical analytics event names plus a tiny JSON payload builder.
    /// Payloads are hand-built strings because JsonUtility cannot serialize
    /// arbitrary dictionaries and the payload shapes here are small and flat.
    /// </summary>
    public static class AnalyticsEvent
    {
        public const string SceneLoaded = "scene_loaded";
        public const string ProductViewed = "product_viewed";
        public const string RecommendationToggled = "recommendation_toggled";
        public const string SofaColorChanged = "sofa_color_changed";
        public const string RoomSizeChanged = "room_size_changed";
        public const string BundleAdded = "bundle_added";
        public const string ConfigurationSaved = "configuration_saved";
        public const string AddToCartClicked = "add_to_cart_clicked";

        /// <summary>Wraps a pre-serialized JSON fragment so Payload() emits it verbatim.</summary>
        public readonly struct RawJson
        {
            public readonly string Json;
            public RawJson(string json) { Json = json; }
        }

        public static RawJson Raw(string json) => new RawJson(json);

        public static string Payload(params (string key, object value)[] fields)
        {
            var sb = new StringBuilder("{");
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(Escape(fields[i].key)).Append("\":");
                AppendValue(sb, fields[i].value);
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string JsonStringArray(IEnumerable<string> values)
        {
            var sb = new StringBuilder("[");
            bool first = true;
            foreach (var v in values)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(Escape(v)).Append('"');
            }
            sb.Append(']');
            return sb.ToString();
        }

        static void AppendValue(StringBuilder sb, object value)
        {
            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case int i:
                    sb.Append(i.ToString(CultureInfo.InvariantCulture));
                    break;
                case float f:
                    sb.Append(f.ToString("0.##", CultureInfo.InvariantCulture));
                    break;
                case double d:
                    sb.Append(d.ToString("0.##", CultureInfo.InvariantCulture));
                    break;
                case RawJson raw:
                    sb.Append(string.IsNullOrEmpty(raw.Json) ? "null" : raw.Json);
                    break;
                default:
                    sb.Append('"').Append(Escape(value.ToString())).Append('"');
                    break;
            }
        }

        static string Escape(string s)
        {
            return s == null
                ? string.Empty
                : s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
