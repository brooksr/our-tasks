import { describe, expect, it } from 'vitest';
import { applyTaskAction } from './actions';
import { createSeedSnapshot } from './seed';

describe('task actions', () => {
  it('snoozes and preserves the previous due date in immutable history', () => {
    const snapshot = createSeedSnapshot();
    const task = snapshot.tasks[0];
    const result = applyTaskAction(snapshot, { taskId: task.id, action: 'snooze', performedBy: 'primary', expectedVersion: 1, nextDueDate: '2026-07-15' }, new Date('2026-07-12T12:00:00Z'));
    expect(result.task.dueDate).toBe('2026-07-15');
    expect(result.event).toMatchObject({ eventType: 'snoozed', previousDueDate: '2026-07-12', nextDueDate: '2026-07-15' });
  });

  it('skips without counting a completion or advancing rotation by default', () => {
    const snapshot = createSeedSnapshot();
    const task = snapshot.tasks[1];
    const result = applyTaskAction(snapshot, { taskId: task.id, action: 'skip', performedBy: 'primary', expectedVersion: 1 }, new Date('2026-07-14T12:00:00Z'));
    expect(result.event.eventType).toBe('skipped');
    expect(result.task.assignedTo).toBe(task.assignedTo);
  });

  it('deducts supplies without producing negative inventory', () => {
    const snapshot = createSeedSnapshot();
    const task = snapshot.tasks[0];
    const supply = snapshot.supplies[0];
    const result = applyTaskAction(snapshot, { taskId: task.id, action: 'complete', performedBy: 'primary', expectedVersion: 1, suppliesUsed: [{ supplyId: supply.id, quantity: 10 }] }, new Date('2026-07-12T12:00:00Z'));
    expect(result.snapshot.supplies[0].quantity).toBe(0);
  });

  it('rotates to whoever did not complete it last', () => {
    const snapshot = createSeedSnapshot();
    const task = snapshot.tasks[0];
    const result = applyTaskAction(snapshot, { taskId: task.id, action: 'complete', performedBy: 'primary', expectedVersion: 1 }, new Date('2026-07-12T12:00:00Z'));
    expect(result.task.assignedTo).toBe('primary');
  });
});
