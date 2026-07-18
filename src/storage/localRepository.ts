import Dexie, { type Table } from 'dexie';
import { applyTaskAction } from '../domain/actions';
import { clearCheckedShopping, newShoppingItem, toggleShopping } from '../domain/shopping';
import { createSeedSnapshot } from '../domain/seed';
import { ConflictError, type HouseholdSnapshot, type Id, type MaintenanceTask, type RepositoryStatus, type TaskActionInput } from '../domain/types';
import type { HouseholdRepository } from './repository';

interface StateRow { key: string; value: HouseholdSnapshot; }
interface QueueRow { id?: number; createdAt: string; type: 'saveTask' | 'action' | 'shoppingUpsert' | 'shoppingDelete'; payload: unknown; }

class HouseholdDb extends Dexie {
  state!: Table<StateRow, string>;
  queue!: Table<QueueRow, number>;
  constructor(name = 'our-tasks-v1') {
    super(name);
    this.version(1).stores({ state: 'key', queue: '++id, createdAt, type' });
  }
}

export class LocalHouseholdRepository implements HouseholdRepository {
  protected db: HouseholdDb;
  protected status: RepositoryStatus = { state: navigator.onLine ? 'synced' : 'offline', pending: 0 };
  private listeners = new Set<(status: RepositoryStatus) => void>();

  constructor(dbName?: string) { this.db = new HouseholdDb(dbName); }

  protected setStatus(patch: Partial<RepositoryStatus>) {
    this.status = { ...this.status, ...patch };
    this.listeners.forEach((listener) => listener(this.status));
  }

  async initialize() {
    const existing = await this.db.state.get('snapshot');
    if (existing) {
      this.setStatus({ pending: await this.db.queue.count(), state: navigator.onLine ? this.status.state : 'offline' });
      return existing.value;
    }
    const snapshot = createSeedSnapshot();
    await this.db.state.put({ key: 'snapshot', value: snapshot });
    return snapshot;
  }

  async getSnapshot() { return (await this.initialize()); }

  protected async persist(snapshot: HouseholdSnapshot) {
    await this.db.state.put({ key: 'snapshot', value: snapshot });
    return snapshot;
  }

  protected async enqueue(type: QueueRow['type'], payload: unknown) {
    await this.db.queue.add({ createdAt: new Date().toISOString(), type, payload });
    this.setStatus({ pending: await this.db.queue.count(), state: navigator.onLine ? 'syncing' : 'offline' });
  }

  async saveTask(task: MaintenanceTask) {
    const snapshot = await this.getSnapshot();
    const current = snapshot.tasks.find((item) => item.id === task.id);
    if (current && current.version !== task.version) throw new ConflictError(task, current);
    const timestamp = new Date().toISOString();
    const saved = { ...task, updatedAt: timestamp, version: current ? current.version + 1 : 1 };
    await this.persist({ ...snapshot, tasks: current ? snapshot.tasks.map((item) => item.id === saved.id ? saved : item) : [saved, ...snapshot.tasks] });
    await this.enqueue('saveTask', saved);
    return saved;
  }

  async performAction(input: TaskActionInput) {
    const snapshot = await this.getSnapshot();
    let result;
    try { result = applyTaskAction(snapshot, input); }
    catch (error) {
      if (error instanceof Error && error.message === 'VERSION_CONFLICT') {
        throw new ConflictError(input, snapshot.tasks.find((task) => task.id === input.taskId));
      }
      throw error;
    }
    await this.persist(result.snapshot);
    await this.enqueue('action', input);
    return result.snapshot;
  }

  async addShoppingItem(name: string, category = 'Other') {
    const trimmed = name.trim();
    if (!trimmed) return this.getSnapshot();
    const snapshot = await this.getSnapshot();
    const item = newShoppingItem(trimmed, category);
    const next = await this.persist({ ...snapshot, shopping: [item, ...snapshot.shopping] });
    await this.enqueue('shoppingUpsert', item);
    return next;
  }

  async toggleShoppingItem(id: Id) {
    const snapshot = await this.getSnapshot();
    const shopping = toggleShopping(snapshot.shopping, id);
    const updated = shopping.find((item) => item.id === id);
    const next = await this.persist({ ...snapshot, shopping });
    if (updated) await this.enqueue('shoppingUpsert', updated);
    return next;
  }

  async clearCheckedShopping() {
    const snapshot = await this.getSnapshot();
    const { items, removed } = clearCheckedShopping(snapshot.shopping);
    const next = await this.persist({ ...snapshot, shopping: items });
    for (const item of removed) await this.enqueue('shoppingDelete', { id: item.id, expectedVersion: item.version });
    return next;
  }

  async sync() {
    this.setStatus({ state: navigator.onLine ? 'synced' : 'offline' });
    return this.getSnapshot();
  }

  getStatus() { return this.status; }
  subscribe(listener: (status: RepositoryStatus) => void) { this.listeners.add(listener); return () => { this.listeners.delete(listener); }; }

  protected async queuedActions() { return this.db.queue.orderBy('id').toArray(); }
  protected async clearQueuedActions() { await this.db.queue.clear(); this.setStatus({ pending: 0 }); }
}
