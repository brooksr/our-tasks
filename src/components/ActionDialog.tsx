import { useEffect, useMemo, useRef, useState } from 'react';
import { calculateNextDueDate, dateKey } from '../domain/recurrence';
import { relatedSupplies } from '../domain/shopping';
import type { MaintenanceTask, Supply, SupplyUsage, TaskActionInput } from '../domain/types';

export function ActionDialog({ task, action, supplies, onClose, onSubmit }: {
  task: MaintenanceTask; action: TaskActionInput['action']; supplies: Supply[];
  onClose: () => void; onSubmit: (input: Omit<TaskActionInput, 'taskId' | 'performedBy' | 'expectedVersion' | 'action'>) => Promise<void>;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const related = useMemo(() => relatedSupplies(task, supplies), [task, supplies]);
  const calculated = useMemo(() => calculateNextDueDate(task, dateKey(new Date())), [task]);
  const [nextDate, setNextDate] = useState(action === 'snooze' ? dateKey(new Date(Date.now() + task.defaultSnoozeDays * 86_400_000)) : calculated ?? '');
  const [notes, setNotes] = useState('');
  const [reason, setReason] = useState('');
  const [cost, setCost] = useState('');
  const [time, setTime] = useState('');
  const [mileage, setMileage] = useState('');
  const [usage, setUsage] = useState<SupplyUsage[]>([]);
  const [saving, setSaving] = useState(false);

  useEffect(() => { ref.current?.showModal(); }, []);
  async function submit(event: React.FormEvent) {
    event.preventDefault(); setSaving(true);
    try {
      await onSubmit({ nextDueDate: nextDate || undefined, notes: notes || undefined, reason: reason || undefined,
        cost: cost ? Number(cost) : undefined, mileage: mileage ? Number(mileage) : undefined,
        timeSpentMinutes: time ? Number(time) : undefined, suppliesUsed: usage.length ? usage : undefined });
    } finally { setSaving(false); }
  }
  const title = action === 'complete' ? 'Complete task' : action === 'snooze' ? 'Snooze task' : 'Skip occurrence';
  return (
    <dialog ref={ref} className="action-dialog" onCancel={onClose} onClose={onClose} aria-labelledby="action-title">
      <form method="dialog" onSubmit={submit}>
        <div className="dialog-head"><div><span className="eyebrow">{task.category}</span><h2 id="action-title">{title}</h2><p>{task.title}</p></div><button type="button" className="icon-button" onClick={onClose} aria-label="Close">×</button></div>
        {action === 'complete' && calculated && <p className="next-date-callout">Calculated next date <strong>{calculated}</strong></p>}
        {(action === 'complete' || action === 'snooze') && <label>Next due date<input type="date" value={nextDate} onChange={(e) => setNextDate(e.target.value)} /><small>Clear for no next date.</small></label>}
        {action !== 'complete' && <label>Reason (optional)<textarea value={reason} onChange={(e) => setReason(e.target.value)} /></label>}
        <label>Notes (optional)<textarea value={notes} onChange={(e) => setNotes(e.target.value)} /></label>
        {action === 'complete' && <div className="form-grid">
          <label>Time spent (minutes)<input inputMode="numeric" type="number" min="0" value={time} onChange={(e) => setTime(e.target.value)} /></label>
          <label>Cost<input inputMode="decimal" type="number" min="0" step="0.01" value={cost} onChange={(e) => setCost(e.target.value)} /></label>
          {task.requiresMileage && <label>Mileage<input inputMode="numeric" type="number" min="0" value={mileage} onChange={(e) => setMileage(e.target.value)} required /></label>}
        </div>}
        {action === 'complete' && related.length > 0 && <details className="supply-details"><summary>Supplies used<small>{usage.length ? `${usage.length} logged` : `${related.length} related`}</small></summary>{related.map((supply) => {
          const current = usage.find((item) => item.supplyId === supply.id)?.quantity ?? '';
          return <label className="supply-use" key={supply.id}><span>{supply.name}<small>{supply.quantity} {supply.unit} available</small></span><input aria-label={`${supply.name} quantity used`} type="number" min="0" step="0.1" value={current} onChange={(e) => setUsage((items) => [...items.filter((item) => item.supplyId !== supply.id), ...(e.target.value ? [{ supplyId: supply.id, quantity: Number(e.target.value) }] : [])])} /></label>;
        })}</details>}
        <div className="dialog-actions"><button type="button" className="secondary" onClick={onClose}>Cancel</button><button type="submit" className="primary" disabled={saving}>{saving ? 'Saving…' : title}</button></div>
      </form>
    </dialog>
  );
}
