import type { HouseholdSnapshot, MaintenanceTask, RepositoryStatus, TaskActionInput } from '../domain/types';

export interface HouseholdRepository {
  initialize(): Promise<HouseholdSnapshot>;
  getSnapshot(): Promise<HouseholdSnapshot>;
  saveTask(task: MaintenanceTask): Promise<MaintenanceTask>;
  performAction(input: TaskActionInput): Promise<HouseholdSnapshot>;
  sync(): Promise<HouseholdSnapshot>;
  getStatus(): RepositoryStatus;
  subscribe(listener: (status: RepositoryStatus) => void): () => void;
}
