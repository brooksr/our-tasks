import type { HouseholdSnapshot, Id, MaintenanceTask, RepositoryStatus, TaskActionInput } from '../domain/types';

export interface HouseholdRepository {
  initialize(): Promise<HouseholdSnapshot>;
  getSnapshot(): Promise<HouseholdSnapshot>;
  saveTask(task: MaintenanceTask): Promise<MaintenanceTask>;
  performAction(input: TaskActionInput): Promise<HouseholdSnapshot>;
  addShoppingItem(name: string, category?: string): Promise<HouseholdSnapshot>;
  toggleShoppingItem(id: Id): Promise<HouseholdSnapshot>;
  clearCheckedShopping(): Promise<HouseholdSnapshot>;
  sync(): Promise<HouseholdSnapshot>;
  getStatus(): RepositoryStatus;
  subscribe(listener: (status: RepositoryStatus) => void): () => void;
}
