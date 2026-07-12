import { useEffect, useRef, useState } from 'react';
import { CATEGORIES } from '../domain/seed';
import type { Asset, MaintenanceTask, RecurrenceType, Room } from '../domain/types';

export function TaskEditorDialog({ task, rooms, assets, onClose, onSave }: {
  task?: MaintenanceTask; rooms: Room[]; assets: Asset[]; onClose: () => void; onSave: (task: MaintenanceTask) => Promise<void>;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const [title, setTitle] = useState(task?.title ?? '');
  const [description, setDescription] = useState(task?.description ?? '');
  const [category, setCategory] = useState(task?.category ?? 'House');
  const [dueDate, setDueDate] = useState(task?.dueDate ?? '');
  const [assignedTo, setAssignedTo] = useState(task?.assignedTo ?? 'either');
  const [roomId, setRoomId] = useState(task?.roomId ?? '');
  const [assetId, setAssetId] = useState(task?.assetId ?? '');
  const [recurrenceType, setRecurrenceType] = useState<RecurrenceType>(task?.recurrenceType ?? 'none');
  const [interval, setInterval] = useState(task?.recurrenceConfig.interval ?? 1);
  const [unit, setUnit] = useState(task?.recurrenceConfig.unit ?? 'months');
  const [priority, setPriority] = useState(task?.priority ?? 'normal');
  const [saving, setSaving] = useState(false);
  useEffect(() => { ref.current?.showModal(); }, []);

  async function submit(event: React.FormEvent) {
    event.preventDefault(); setSaving(true);
    const timestamp = new Date().toISOString();
    try {
      await onSave({
        ...(task ?? {
          id: `task_${globalThis.crypto?.randomUUID?.() ?? Date.now()}`, active: true, createdAt: timestamp,
          updatedAt: timestamp, version: 0, locationIds: [], unityObjectIds: [], assignmentMode: 'not_last',
          status: 'open', nextDateStrategy: 'scheduled_date', seasonalRegion: 'Thousand Oaks, CA',
          defaultSnoozeDays: 3, requiresReading: false, requiresMileage: false, requiresUsageCount: false, createdBy: 'primary'
        }),
        title: title.trim(), description: description.trim(), category, dueDate: dueDate || undefined,
        assignedTo, roomId: roomId || undefined, assetId: assetId || undefined,
        recurrenceType, recurrenceConfig: recurrenceType === 'interval' ? { interval, unit } : task?.recurrenceConfig ?? {}, priority
      } as MaintenanceTask);
    } finally { setSaving(false); }
  }

  return <dialog ref={ref} className="action-dialog" onCancel={onClose} onClose={onClose} aria-labelledby="editor-title"><form onSubmit={submit}>
    <div className="dialog-head"><div><span className="eyebrow">Household task</span><h2 id="editor-title">{task ? 'Edit task' : 'New task'}</h2></div><button type="button" className="icon-button" onClick={onClose} aria-label="Close">×</button></div>
    <label>Title<input value={title} onChange={(e) => setTitle(e.target.value)} required maxLength={160} autoFocus /></label>
    <label>Description<textarea value={description} onChange={(e) => setDescription(e.target.value)} maxLength={2000} /></label>
    <div className="form-grid"><label>Category<select value={category} onChange={(e) => setCategory(e.target.value)}>{CATEGORIES.map((value) => <option key={value}>{value}</option>)}</select></label><label>Priority<select value={priority} onChange={(e) => setPriority(e.target.value as MaintenanceTask['priority'])}><option>low</option><option>normal</option><option>high</option><option>urgent</option></select></label><label>Assigned to<select value={assignedTo} onChange={(e) => setAssignedTo(e.target.value as MaintenanceTask['assignedTo'])}><option value="primary">Brooks</option><option value="secondary">Wife</option><option value="either">Either</option><option value="shared">Shared</option></select></label><label>Due date<input type="date" value={dueDate} onChange={(e) => setDueDate(e.target.value)} /></label><label>Room<select value={roomId} onChange={(e) => setRoomId(e.target.value)}><option value="">No room</option>{rooms.map((room) => <option value={room.id} key={room.id}>{room.name}</option>)}</select></label><label>Asset<select value={assetId} onChange={(e) => setAssetId(e.target.value)}><option value="">No asset</option>{assets.map((asset) => <option value={asset.id} key={asset.id}>{asset.name}</option>)}</select></label></div>
    <label>Recurrence<select value={recurrenceType} onChange={(e) => setRecurrenceType(e.target.value as RecurrenceType)}><option value="none">One time</option><option value="on_demand">On demand</option><option value="interval">Fixed interval</option><option value="weekly">Weekly pattern</option><option value="monthly">Monthly pattern</option><option value="annual">Annual</option><option value="seasonal">Seasonal</option><option value="mileage">Mileage or date</option><option value="usage">Usage or date</option><option value="threshold">Condition threshold</option></select></label>
    {recurrenceType === 'interval' && <div className="form-grid"><label>Every<input type="number" min="1" max="999" value={interval} onChange={(e) => setInterval(Number(e.target.value))} /></label><label>Unit<select value={unit} onChange={(e) => setUnit(e.target.value as typeof unit)}><option value="days">days</option><option value="weeks">weeks</option><option value="months">months</option><option value="years">years</option></select></label></div>}
    <div className="dialog-actions"><button type="button" className="secondary" onClick={onClose}>Cancel</button><button type="submit" className="primary" disabled={saving || !title.trim()}>{saving ? 'Saving…' : 'Save task'}</button></div>
  </form></dialog>;
}
