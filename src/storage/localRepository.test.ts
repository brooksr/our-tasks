import 'fake-indexeddb/auto';
import { afterEach, describe, expect, it } from 'vitest';
import Dexie from 'dexie';
import { LocalHouseholdRepository } from './localRepository';

const names: string[] = [];
afterEach(async () => { await Promise.all(names.splice(0).map((name) => Dexie.delete(name))); });

describe('offline repository', () => {
  it('persists a queued action and retains it across repository instances', async () => {
    const name = `test-${Math.random()}`; names.push(name);
    const first = new LocalHouseholdRepository(name);
    const snapshot = await first.initialize();
    const task = snapshot.tasks[0];
    await first.performAction({ taskId: task.id, action: 'snooze', performedBy: 'primary', expectedVersion: task.version, nextDueDate: '2026-07-20' });
    expect(first.getStatus().pending).toBe(1);
    const second = new LocalHouseholdRepository(name);
    const restored = await second.initialize();
    expect(restored.tasks.find((item) => item.id === task.id)?.dueDate).toBe('2026-07-20');
    expect(second.getStatus().pending).toBe(1);
  });
});
