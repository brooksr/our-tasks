import type { Id, MaintenanceTask, ShoppingItem, Supply } from './types';

function id(prefix: string) {
  return `${prefix}_${globalThis.crypto?.randomUUID?.() ?? Math.random().toString(36).slice(2)}`;
}

/** Build a new, unchecked shopping item. */
export function newShoppingItem(name: string, category = 'Other', addedBy: Id = 'primary'): ShoppingItem {
  const now = new Date().toISOString();
  return { id: id('shop'), name: name.trim(), category, checked: false, addedBy, active: true, createdAt: now, updatedAt: now, version: 1 };
}

function touch(item: ShoppingItem, patch: Partial<ShoppingItem>): ShoppingItem {
  return { ...item, ...patch, updatedAt: new Date().toISOString(), version: item.version + 1 };
}

export function toggleShopping(items: ShoppingItem[], itemId: Id): ShoppingItem[] {
  return items.map((item) => item.id === itemId ? touch(item, { checked: !item.checked }) : item);
}

/** Soft-delete checked items so the change syncs like other deletes. */
export function clearCheckedShopping(items: ShoppingItem[]): { items: ShoppingItem[]; removed: ShoppingItem[] } {
  const removed = items.filter((item) => item.active && item.checked);
  const now = new Date().toISOString();
  return {
    items: items.map((item) => item.checked && item.active ? { ...item, active: false, deletedAt: now, updatedAt: now, version: item.version + 1 } : item),
    removed
  };
}

/** Supplies relevant to a task: same tracked asset, or same category. */
export function relatedSupplies(task: MaintenanceTask, supplies: Supply[]): Supply[] {
  return supplies.filter((supply) => supply.active && (
    (supply.assetId && supply.assetId === task.assetId) || supply.category === task.category
  ));
}
