import { Check, Plus, Trash2 } from 'lucide-react';
import { useMemo, useState } from 'react';
import { SHOPPING_CATEGORIES } from '../domain/seed';
import type { Id, ShoppingItem } from '../domain/types';

const OTHER = 'Other';
function order(category: string) {
  const index = SHOPPING_CATEGORIES.indexOf(category);
  return index < 0 ? SHOPPING_CATEGORIES.length : index;
}

export function ShoppingList({ items, onAdd, onToggle, onClearChecked }: {
  items: ShoppingItem[];
  onAdd: (name: string, category: string) => void;
  onToggle: (id: Id) => void;
  onClearChecked: () => void;
}) {
  const [name, setName] = useState('');
  const [category, setCategory] = useState(SHOPPING_CATEGORIES[0]);

  const active = useMemo(() => items.filter((item) => item.active), [items]);
  const remaining = active.filter((item) => !item.checked).length;
  const checked = active.filter((item) => item.checked).length;

  const grouped = useMemo(() => {
    const map = new Map<string, ShoppingItem[]>();
    for (const item of active) {
      const key = item.category || OTHER;
      (map.get(key) ?? map.set(key, []).get(key)!).push(item);
    }
    return [...map.entries()]
      .map(([label, list]) => [label, list.sort((a, b) => a.name.localeCompare(b.name))] as const)
      .sort(([a], [b]) => order(a) - order(b) || a.localeCompare(b));
  }, [active]);

  function add(event: React.FormEvent) {
    event.preventDefault();
    if (!name.trim()) return;
    onAdd(name, category);
    setName('');
  }

  const categoryOptions = [...new Set([...SHOPPING_CATEGORIES, OTHER])];

  return (
    <section className="page-view">
      <div className="page-heading">
        <div><span className="eyebrow">Shared list</span><h1>Shopping</h1><p>{remaining} to buy{checked ? ` · ${checked} in the cart` : ''}.</p></div>
        {checked > 0 && <button className="secondary" type="button" onClick={onClearChecked}><Trash2 aria-hidden="true" /> Clear {checked} checked</button>}
      </div>

      <form className="shopping-add" onSubmit={add}>
        <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Add an item…" aria-label="Item name" />
        <select value={category} onChange={(e) => setCategory(e.target.value)} aria-label="Category">{categoryOptions.map((option) => <option key={option}>{option}</option>)}</select>
        <button className="primary" type="submit"><Plus aria-hidden="true" /> Add</button>
      </form>

      {active.length === 0
        ? <div className="empty-state"><Check aria-hidden="true" /><p>The list is empty. Add something above.</p></div>
        : <div className="shopping-groups">{grouped.map(([label, list]) => (
            <section className="shopping-group" key={label}>
              <h2>{label}<span>{list.filter((item) => !item.checked).length}</span></h2>
              <ul>{list.map((item) => (
                <li key={item.id}>
                  <label className={`shopping-item${item.checked ? ' checked' : ''}`}>
                    <input type="checkbox" checked={item.checked} onChange={() => onToggle(item.id)} aria-label={item.name} />
                    <span className="check-box" aria-hidden="true"><Check /></span>
                    <span className="shopping-name">{item.name}{item.note ? <small>{item.note}</small> : null}</span>
                  </label>
                </li>
              ))}</ul>
            </section>
          ))}</div>}
    </section>
  );
}
