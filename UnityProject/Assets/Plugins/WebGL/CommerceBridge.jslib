// Unity → JavaScript half of the commerce bridge.
// WebGLBridge.cs P/Invokes CommerceBridge_Emit; this forwards the event to the
// host page's window.UnityCommerceBridge.handleUnityEvent(eventName, payload).
// If no host bridge is present (e.g. the raw Unity build opened directly),
// events fall back to console.log so they are never silently lost.
mergeInto(LibraryManager.library, {
  CommerceBridge_Emit: function (eventNamePtr, payloadJsonPtr) {
    var eventName = UTF8ToString(eventNamePtr);
    var payloadJson = UTF8ToString(payloadJsonPtr);
    var payload = {};
    try {
      payload = payloadJson ? JSON.parse(payloadJson) : {};
    } catch (e) {
      payload = { raw: payloadJson };
    }

    if (
      typeof window !== 'undefined' &&
      window.UnityCommerceBridge &&
      typeof window.UnityCommerceBridge.handleUnityEvent === 'function'
    ) {
      window.UnityCommerceBridge.handleUnityEvent(eventName, payload);
    } else {
      console.log('[unity-event]', eventName, payload);
    }
  }
});
