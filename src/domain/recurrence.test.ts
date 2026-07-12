import { describe, expect, it } from 'vitest';
import { calculateNextDueDate, dueState, evaluateCounterTrigger } from './recurrence';
import { createSeedSnapshot } from './seed';
import type { MaintenanceTask } from './types';

function example(patch: Partial<MaintenanceTask>): MaintenanceTask {
  return { ...createSeedSnapshot().tasks[0], recurrenceType: 'interval', recurrenceConfig: { interval: 1, unit: 'months' }, nextDateStrategy: 'completion_date', ...patch };
}

describe('calculateNextDueDate', () => {
  it('clamps month-end schedules', () => expect(calculateNextDueDate(example({}), '2025-01-31')).toBe('2025-02-28'));
  it('handles leap years', () => expect(calculateNextDueDate(example({ recurrenceConfig: { interval: 1, unit: 'years' } }), '2024-02-29')).toBe('2025-02-28'));
  it('uses the scheduled date for fixed cadence', () => expect(calculateNextDueDate(example({ dueDate: '2026-07-01', nextDateStrategy: 'scheduled_date' }), '2026-07-12')).toBe('2026-08-01'));
  it('uses completion date for completion-relative cadence', () => expect(calculateNextDueDate(example({ dueDate: '2026-07-01' }), '2026-07-12')).toBe('2026-08-12'));
  it('finds the last Sunday of a later month', () => expect(calculateNextDueDate(example({ recurrenceType: 'monthly', recurrenceConfig: { interval: 3, weekday: 0, weekdayOrdinal: -1 } }), '2026-01-05')).toBe('2026-04-26'));
  it('handles dates across daylight-saving transitions without shifting the date', () => expect(calculateNextDueDate(example({ recurrenceConfig: { interval: 1, unit: 'days' } }), '2026-03-08')).toBe('2026-03-09'));
  it('supports multiple annual dates', () => expect(calculateNextDueDate(example({ recurrenceType: 'annual', recurrenceConfig: { annualDates: ['04-01', '10-01'] } }), '2026-04-01')).toBe('2026-10-01'));
});

describe('due states and counters', () => {
  it('uses due windows without changing target status semantics', () => expect(dueState(example({ dueDate: '2026-07-19', dueWindowStart: '2026-07-17', dueWindowEnd: '2026-07-21' }), '2026-07-12')).toBe('soon'));
  it('triggers mileage and usage thresholds', () => {
    expect(evaluateCounterTrigger(example({ recurrenceType: 'mileage', recurrenceConfig: { mileageInterval: 7500 } }), 7500)).toBe(true);
    expect(evaluateCounterTrigger(example({ recurrenceType: 'usage', recurrenceConfig: { usageInterval: 20 } }), 19)).toBe(false);
  });
});
