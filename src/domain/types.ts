export type Id = string;
export type Priority = 'low' | 'normal' | 'high' | 'urgent';
export type TaskStatus = 'open' | 'completed' | 'snoozed' | 'skipped' | 'archived';
export type Assignee = 'primary' | 'secondary' | 'either' | 'shared';
export type AssignmentMode = 'fixed' | 'either' | 'shared' | 'alternate' | 'not_last' | 'manual_rotation' | 'lowest_workload';
export type NextDateStrategy = 'completion_date' | 'scheduled_date' | 'user_choice';
export type RecurrenceType = 'none' | 'on_demand' | 'interval' | 'weekly' | 'monthly' | 'annual' | 'seasonal' | 'mileage' | 'usage' | 'threshold';
export type SyncState = 'synced' | 'syncing' | 'offline' | 'conflict' | 'error';
export type TaskEventType = 'created' | 'edited' | 'completed' | 'skipped' | 'snoozed' | 'reopened' | 'assigned' | 'archived' | 'restored' | 'corrected';

export interface VersionedEntity {
  id: Id;
  active: boolean;
  createdAt: string;
  updatedAt: string;
  version: number;
  deletedAt?: string;
}

export interface HouseholdUser extends VersionedEntity {
  email: string;
  displayName: string;
  role: 'member' | 'admin';
}

export interface Room extends VersionedEntity {
  name: string;
  floor: string;
  zone: string;
  unityObjectId?: string;
  notes?: string;
}

export interface Asset extends VersionedEntity {
  name: string;
  type: string;
  category: string;
  roomId?: Id;
  locationId?: Id;
  unityObjectId?: string;
  smartDeviceId?: string;
  smartHomeProvider?: string;
  manufacturer?: string;
  model?: string;
  serialNumber?: string;
  purchaseDate?: string;
  warrantyExpiration?: string;
  manualUrl?: string;
  photoUrl?: string;
  notes?: string;
}

export interface RecurrenceConfig {
  interval?: number;
  unit?: 'days' | 'weeks' | 'months' | 'years';
  weekdays?: number[];
  dayOfMonth?: number | 'last';
  weekdayOrdinal?: 1 | 2 | 3 | 4 | -1;
  weekday?: number;
  annualDates?: string[];
  months?: number[];
  mileageInterval?: number;
  usageInterval?: number;
  calendarFallback?: { interval: number; unit: 'days' | 'weeks' | 'months' | 'years' };
  threshold?: { source: 'supply' | 'manual' | 'sensor' | 'weather'; operator: 'lt' | 'lte' | 'gt' | 'gte' | 'eq'; value: number };
  skipAdvancesRotation?: boolean;
  rotationOrder?: Assignee[];
}

export interface MaintenanceTask extends VersionedEntity {
  title: string;
  description: string;
  category: string;
  roomId?: Id;
  assetId?: Id;
  locationIds: Id[];
  unityObjectIds: string[];
  assignedTo: Assignee;
  assignmentMode: AssignmentMode;
  priority: Priority;
  status: TaskStatus;
  dueDate?: string;
  dueWindowStart?: string;
  dueWindowEnd?: string;
  recurrenceType: RecurrenceType;
  recurrenceConfig: RecurrenceConfig;
  nextDateStrategy: NextDateStrategy;
  seasonalRegion: string;
  estimatedMinutes?: number;
  defaultSnoozeDays: number;
  requiresReading: boolean;
  requiresMileage: boolean;
  requiresUsageCount: boolean;
  currentMileage?: number;
  currentUsageCount?: number;
  createdBy: Id;
}

export interface Supply extends VersionedEntity {
  name: string;
  category: string;
  assetId?: Id;
  unit: string;
  quantity: number;
  reorderThreshold: number;
  reorderQuantity: number;
  preferredProductUrl?: string;
  notes?: string;
}

export interface SupplyUsage { supplyId: Id; quantity: number; }

export interface TaskEvent {
  id: Id;
  taskId: Id;
  eventType: TaskEventType;
  eventDate: string;
  performedBy: Id;
  previousDueDate?: string;
  nextDueDate?: string;
  notes?: string;
  reason?: string;
  cost?: number;
  mileage?: number;
  usageCount?: number;
  timeSpentMinutes?: number;
  readings?: Record<string, string | number>;
  suppliesUsed?: SupplyUsage[];
  attachmentUrls?: string[];
  createdAt: string;
}

export interface HouseholdSnapshot {
  users: HouseholdUser[];
  rooms: Room[];
  assets: Asset[];
  tasks: MaintenanceTask[];
  events: TaskEvent[];
  supplies: Supply[];
}

export interface TaskActionInput {
  taskId: Id;
  action: 'complete' | 'snooze' | 'skip' | 'archive' | 'reassign';
  performedBy: Id;
  expectedVersion: number;
  nextDueDate?: string;
  snoozeDays?: number;
  assignedTo?: Assignee;
  notes?: string;
  reason?: string;
  cost?: number;
  mileage?: number;
  usageCount?: number;
  timeSpentMinutes?: number;
  readings?: Record<string, string | number>;
  suppliesUsed?: SupplyUsage[];
  attachmentUrls?: string[];
}

export interface RepositoryStatus { state: SyncState; pending: number; message?: string; }

export class ConflictError extends Error {
  constructor(public local: unknown, public server: unknown) {
    super('This record changed on another device.');
    this.name = 'ConflictError';
  }
}
