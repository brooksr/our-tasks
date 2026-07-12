import type { MaintenanceTask } from '../domain/types';
import { dueState } from '../domain/recurrence';

export type MaintenanceStatusPayload = Array<{ taskId: string; assetId?: string; roomId?: string; unityObjectIds: string[]; state: string }>;

export class UnityMaintenanceBridge {
  constructor(private frame: HTMLIFrameElement | null) {}

  private send(method: string, payload?: unknown) {
    if (!this.frame?.contentWindow) return false;
    this.frame.contentWindow.postMessage({ source: 'our-tasks', method, payload }, window.location.origin);
    return true;
  }

  highlightAsset(assetId: string) { return this.send('highlightAsset', assetId); }
  highlightRoom(roomId: string) { return this.send('highlightRoom', roomId); }
  enterMaintenanceMode() { return this.send('enterMaintenanceMode'); }
  exitMaintenanceMode() { return this.send('exitMaintenanceMode'); }
  refreshStatuses(tasks: MaintenanceTask[]) {
    return this.send('refreshStatuses', tasks.filter((task) => task.active && task.status !== 'archived').map((task) => ({
      taskId: task.id, assetId: task.assetId, roomId: task.roomId,
      unityObjectIds: task.unityObjectIds, state: dueState(task)
    })));
  }
}

export function isValidUnitySelection(value: unknown): value is { assetId?: string; roomId?: string; unityObjectId?: string } {
  if (!value || typeof value !== 'object') return false;
  return ['assetId', 'roomId', 'unityObjectId'].every((key) => !(key in value) || typeof (value as Record<string, unknown>)[key] === 'string');
}
