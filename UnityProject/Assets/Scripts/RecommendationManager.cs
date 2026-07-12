using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CommerceDemo
{
    /// <summary>
    /// Tracks which recommended products are selected, keeps the 3D scene in
    /// sync, and computes the live bundle total (main product + selections).
    /// Selection state is the single source of truth Unity reports back to the
    /// host page in analytics events.
    /// </summary>
    public class RecommendationManager : MonoBehaviour
    {
        CommerceAppController _app;
        RecommendationSet _set;
        Product _mainProduct;
        readonly Dictionary<string, bool> _selected = new Dictionary<string, bool>();

        CommerceAppController App => _app != null ? _app : (_app = GetComponent<CommerceAppController>());

        public void Initialize(RecommendationSet set, Product mainProduct)
        {
            _set = set;
            _mainProduct = mainProduct;
            _selected.Clear();
            if (_set?.items == null) return;

            foreach (var item in _set.items)
            {
                _selected[item.id] = item.selectedDefault;
                App.Room.SetObjectVisible(item.id, item.selectedDefault);
            }
        }

        public void SetSelected(string productId, bool selected, bool emitEvent)
        {
            if (_set?.items == null || _set.items.All(i => i.id != productId))
            {
                Debug.LogWarning($"[Recommendations] Ignoring toggle for unknown product id '{productId}'.");
                return;
            }

            _selected[productId] = selected;
            App.Room.SetObjectVisible(productId, selected);

            if (emitEvent)
            {
                App.Bridge.Emit(AnalyticsEvent.RecommendationToggled, AnalyticsEvent.Payload(
                    ("productId", productId),
                    ("selected", selected),
                    ("selectedCount", SelectedIds.Count),
                    ("bundleTotal", BundleTotal)));
            }
        }

        public bool IsSelected(string productId)
        {
            return _selected.TryGetValue(productId, out var value) && value;
        }

        public List<string> SelectedIds
        {
            get
            {
                if (_set?.items == null) return new List<string>();
                return _set.items.Where(i => IsSelected(i.id)).Select(i => i.id).ToList();
            }
        }

        public float BundleTotal
        {
            get
            {
                float total = _mainProduct != null ? _mainProduct.price : 0f;
                if (_set?.items != null)
                {
                    total += _set.items.Where(i => IsSelected(i.id)).Sum(i => i.price);
                }
                return Mathf.Round(total * 100f) / 100f;
            }
        }

        public void ResetToDefaults()
        {
            if (_set?.items == null) return;
            foreach (var item in _set.items)
            {
                SetSelected(item.id, item.selectedDefault, emitEvent: false);
            }
        }
    }
}
