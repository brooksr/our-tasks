import type { Assignee, MaintenanceTask, TaskEvent } from './types';

export function nextAssignee(task: MaintenanceTask, events: TaskEvent[], workloads: Record<string, number> = {}) : Assignee {
  if (task.assignmentMode === 'fixed' || task.assignmentMode === 'either' || task.assignmentMode === 'shared') return task.assignedTo;
  if (task.assignmentMode === 'lowest_workload') return (workloads.primary ?? 0) <= (workloads.secondary ?? 0) ? 'primary' : 'secondary';
  const completions = events
    .filter((event) => event.taskId === task.id && event.eventType === 'completed')
    .sort((a, b) => b.eventDate.localeCompare(a.eventDate));
  if (task.assignmentMode === 'manual_rotation') {
    const order: Assignee[] = task.recurrenceConfig.rotationOrder?.filter((person) => person === 'primary' || person === 'secondary') ?? ['primary', 'secondary'];
    const index = order.indexOf(task.assignedTo);
    return order[(index + 1) % order.length];
  }
  const last = completions[0]?.performedBy;
  return last === 'primary' ? 'secondary' : 'primary';
}
