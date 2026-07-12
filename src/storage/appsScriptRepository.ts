import { APP_CONFIG } from '../config';
import { ConflictError, type HouseholdSnapshot, type MaintenanceTask, type TaskActionInput } from '../domain/types';
import { getGoogleCredential } from './googleAuth';
import { LocalHouseholdRepository } from './localRepository';

interface ApiResponse<T> { success: boolean; data: T; error?: { code: string; message: string; details?: unknown }; serverTime: string; }

export class AppsScriptHouseholdRepository extends LocalHouseholdRepository {
  private async request<T>(action: string, payload: unknown = {}) {
    const idToken = getGoogleCredential();
    if (!idToken) throw new Error('Sign in with an approved Google account first.');
    const response = await fetch(APP_CONFIG.appsScriptEndpoint, {
      method: 'POST',
      headers: { 'Content-Type': 'text/plain;charset=utf-8' },
      body: JSON.stringify({ action, idToken, payload })
    });
    if (!response.ok) throw new Error(`Backend returned HTTP ${response.status}.`);
    const result = await response.json() as ApiResponse<T>;
    if (!result.success) {
      if (result.error?.code === 'VERSION_CONFLICT') throw new ConflictError(payload, result.error.details);
      throw new Error(result.error?.message || 'The household service returned an error.');
    }
    return result.data;
  }

  override async initialize() {
    const cached = await super.initialize();
    if (!navigator.onLine || !getGoogleCredential()) return cached;
    try { return await this.sync(); } catch { return cached; }
  }

  override async sync() {
    if (!navigator.onLine) { this.setStatus({ state: 'offline' }); return this.getSnapshot(); }
    this.setStatus({ state: 'syncing' });
    try {
      const queue = await this.queuedActions();
      for (const item of queue) {
        await this.request(item.type === 'action' ? 'task.action' : 'tasks.upsert', item.payload);
      }
      await this.clearQueuedActions();
      const snapshot = await this.request<HouseholdSnapshot>('bootstrap');
      await this.persist(snapshot);
      this.setStatus({ state: 'synced', message: undefined });
      return snapshot;
    } catch (error) {
      const state = error instanceof ConflictError ? 'conflict' : navigator.onLine ? 'error' : 'offline';
      this.setStatus({ state, message: error instanceof Error ? error.message : 'Sync failed.' });
      throw error;
    }
  }

  override async saveTask(task: MaintenanceTask) {
    const saved = await super.saveTask(task);
    if (navigator.onLine) await this.sync();
    return saved;
  }

  override async performAction(input: TaskActionInput) {
    const snapshot = await super.performAction(input);
    if (navigator.onLine) await this.sync();
    return snapshot;
  }
}
