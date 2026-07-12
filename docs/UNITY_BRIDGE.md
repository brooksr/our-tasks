# Unity ↔ web maintenance bridge

The host loads the house in a same-origin iframe only when requested. Messages
are plain structured data, origin-checked, and safely ignored before Unity is
ready. Neither side sends credentials or household records beyond task status
and stable mapping IDs.

## Web to Unity

`src/unity/bridge.ts` posts:

```ts
unityMaintenanceBridge.highlightAsset('asset-hot-tub');
unityMaintenanceBridge.highlightRoom('room-backyard');
unityMaintenanceBridge.enterMaintenanceMode();
unityMaintenanceBridge.exitMaintenanceMode();
unityMaintenanceBridge.refreshStatuses(tasks);
```

The frame translates those to `SendMessage('MaintenanceBridge', method,
payload)`. The public C# methods are `HighlightAsset`, `HighlightRoom`,
`EnterMaintenanceMode`, `ExitMaintenanceMode`, and `RefreshStatuses`. Status
payloads are JSON arrays:

```json
[{"taskId":"task-hot-tub-test","assetId":"asset-hot-tub","roomId":"room-backyard","unityObjectIds":["BackyardHotTub"],"state":"overdue"}]
```

`HomeAsset` stores a generated stable ID plus database and Unity object IDs.
Maintenance mode subdues neutral mappings and uses emission plus state text in
object names for non-color-only diagnostics. The HTML task panel remains the
accessible source of status labels.

## Unity to web

Clicking a mapped collider calls the WebGL plugin and then:

```js
window.onUnityAssetSelected({
  assetId: 'asset-hot-tub',
  roomId: 'room-backyard',
  unityObjectId: 'BackyardHotTub'
});
```

The frame forwards `{ source: 'unity-house', type: 'asset-selected', payload }`
to its parent. The React host checks same origin and validates every optional ID
as a string before opening matching maintenance data.

## Mapping workflow

1. Add `HomeAsset` to a selectable GameObject with a collider.
2. Use sheet IDs rather than hierarchy paths. The component's `stableId` is
   created once and survives GameObject renames.
3. Multiple objects can share an `assetId`; a task can list several
   `unityObjectIds`. Room-only tasks set `roomId` without an asset.
4. Run **Tools → Our Tasks → Validate asset mappings** to find missing or
   duplicate stable IDs and unmapped components.
5. Rebuild WebGL. The bridge bootstrap creates its GameObject after scene load,
   so the existing scene hierarchy is not replaced.

The checked-in binary includes these C# additions. The bridge is live, while
scene-specific highlights depend on adding `HomeAsset` mappings to the desired
objects and rebuilding after those mappings are authored.
