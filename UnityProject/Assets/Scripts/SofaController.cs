using System.Collections.Generic;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Manages upholstery color changes for the main product. Uses two owned
    /// material instances (body + slightly darker cushions) so recoloring is
    /// two material.color writes, not a per-renderer material churn.
    /// </summary>
    public class SofaController : MonoBehaviour
    {
        CommerceAppController _app;
        List<Renderer> _upholstery = new List<Renderer>();
        Material _bodyMaterial;
        Material _cushionMaterial;
        string _currentHex;
        string _defaultHex = "#C9B79C";

        CommerceAppController App => _app != null ? _app : (_app = GetComponent<CommerceAppController>());

        public string CurrentColorHex => _currentHex ?? _defaultHex;

        /// <summary>Called by the room controller after the sofa is (re)built.</summary>
        public void Bind(IEnumerable<Renderer> upholsteryRenderers, string defaultHex)
        {
            _upholstery = new List<Renderer>(upholsteryRenderers);
            _defaultHex = NormalizeHex(defaultHex, _defaultHex);
            _bodyMaterial = null;
            _cushionMaterial = null;
            ApplyColor(_defaultHex, emitEvent: false);
        }

        /// <summary>Entry point for SendMessage from the web shell.</summary>
        public void ApplyColor(string hex)
        {
            ApplyColor(hex, emitEvent: true);
        }

        public void ApplyColor(string hex, bool emitEvent)
        {
            if (_upholstery.Count == 0) return;

            hex = NormalizeHex(hex, _defaultHex);
            Color color = SceneObjectFactory.ParseColor(hex, Color.gray);

            if (_bodyMaterial == null)
            {
                _bodyMaterial = SceneObjectFactory.CreateMaterial(color);
                _cushionMaterial = SceneObjectFactory.CreateMaterial(color);
                foreach (var r in _upholstery)
                {
                    if (r == null) continue;
                    bool isCushion = r.name.Contains("Cushion");
                    r.sharedMaterial = isCushion ? _cushionMaterial : _bodyMaterial;
                }
            }

            _bodyMaterial.color = color;
            _cushionMaterial.color = new Color(color.r * 0.88f, color.g * 0.88f, color.b * 0.88f, 1f);
            _currentHex = hex;

            if (emitEvent)
            {
                App.Bridge.Emit(AnalyticsEvent.SofaColorChanged, AnalyticsEvent.Payload(("color", hex)));
            }
        }

        public void ResetColor()
        {
            ApplyColor(_defaultHex, emitEvent: false);
        }

        static string NormalizeHex(string hex, string fallback)
        {
            if (string.IsNullOrEmpty(hex)) return fallback;
            return hex.StartsWith("#") ? hex : "#" + hex;
        }
    }
}
