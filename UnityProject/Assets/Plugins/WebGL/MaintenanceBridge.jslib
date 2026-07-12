mergeInto(LibraryManager.library, {
  MaintenanceAssetSelected: function (payloadPtr) {
    var payload;
    try { payload = JSON.parse(UTF8ToString(payloadPtr)); }
    catch (error) { console.warn('[maintenance-bridge] Invalid Unity payload.', error); return; }
    if (window.onUnityAssetSelected) window.onUnityAssetSelected(payload);
  }
});
