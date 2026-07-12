import { nextAssignee } from './assignment';
import { calculateNextDueDate, dateKey } from './recurrence';
import type { HouseholdSnapshot, MaintenanceTask, Supply, SupplyUsage, TaskActionInput, TaskEvent } from './types';

function id(prefix: string) {
  return `${prefix}_${globalThis.crypto?.randomUUID?.() ?? Math.random().toString(36).slice(2)}`;
}

export function applySupplyUsage(supplies: Supply[], usage: SupplyUsage[] = []) {
  const used = new Map(usage.map((item) => [item.supplyId, item.quantity]));
  return supplies.map((supply) => used.has(supply.id)
    ? { ...supply, quantity: Math.max(0, supply.quantity - (used.get(supply.id) ?? 0)), updatedAt: new Date().toISOString(), version: supply.version + 1 }
    : supply);
}

export function applyTaskAction(snapshot: HouseholdSnapshot, input: TaskActionInput, now = new Date()) {
  const task = snapshot.tasks.find((item) => item.id === input.taskId);
  if (!task) throw new Error('Task not found.');
  if (task.version !== input.expectedVersion) throw new Error('VERSION_CONFLICT');

  const timestamp = now.toISOString();
  const eventType = input.action === 'complete' ? 'completed' : input.action === 'archive' ? 'archived' : input.action === 'reassign' ? 'assigned' : input.action === 'snooze' ? 'snoozed' : 'skipped';
  let nextDueDate = task.dueDate;
  let status: MaintenanceTask['status'] = task.status;
  let assignedTo = task.assignedTo;

  if (input.action === 'complete') {
    nextDueDate = input.nextDueDate ?? calculateNextDueDate(task, dateKey(now));
    status = nextDueDate || task.recurrenceType === 'on_demand' ? 'open' : 'completed';
    assignedTo = nextAssignee(task, snapshot.events);
  } else if (input.action === 'snooze') {
    const days = input.snoozeDays ?? task.defaultSnoozeDays;
    nextDueDate = input.nextDueDate ?? dateKey(new Date(now.getTime() + days * 86_400_000));
    status = 'snoozed';
  } else if (input.action === 'skip') {
    nextDueDate = calculateNextDueDate(task, dateKey(now), task.dueDate);
    status = nextDueDate ? 'open' : 'skipped';
    if (task.recurrenceConfig.skipAdvancesRotation) assignedTo = nextAssignee(task, snapshot.events);
  } else if (input.action === 'archive') {
    status = 'archived';
  } else if (input.assignedTo) {
    assignedTo = input.assignedTo;
  }

  const updated: MaintenanceTask = {
    ...task,
    assignedTo,
    dueDate: nextDueDate,
    status,
    active: status !== 'archived',
    deletedAt: status === 'archived' ? timestamp : task.deletedAt,
    updatedAt: timestamp,
    version: task.version + 1
  };
  const event: TaskEvent = {
    id: id('event'), taskId: task.id, eventType, eventDate: timestamp,
    performedBy: input.performedBy, previousDueDate: task.dueDate,
    nextDueDate, notes: input.notes, reason: input.reason, cost: input.cost,
    mileage: input.mileage, usageCount: input.usageCount,
    timeSpentMinutes: input.timeSpentMinutes, readings: input.readings,
    suppliesUsed: input.suppliesUsed, attachmentUrls: input.attachmentUrls,
    createdAt: timestamp
  };
  return {
    snapshot: {
      ...snapshot,
      tasks: snapshot.tasks.map((item) => item.id === task.id ? updated : item),
      events: [event, ...snapshot.events],
      supplies: input.action === 'complete' ? applySupplyUsage(snapshot.supplies, input.suppliesUsed) : snapshot.supplies
    },
    task: updated,
    event
  };
}
