import { AlertTriangle, CalendarClock, Check, Clock3, History, MapPin, MoreHorizontal, PackageOpen, UserRound } from 'lucide-react';
import { APP_CONFIG } from '../config';
import { dueState } from '../domain/recurrence';
import type { Asset, MaintenanceTask, Room } from '../domain/types';

export function TaskCard({ task, assets, rooms, onAction, onHistory, onEdit }: {
  task: MaintenanceTask; assets: Asset[]; rooms: Room[];
  onAction: (task: MaintenanceTask, action: 'complete' | 'snooze' | 'skip') => void;
  onHistory: (task: MaintenanceTask) => void; onEdit: (task: MaintenanceTask) => void;
}) {
  const state = dueState(task);
  const room = rooms.find((item) => item.id === task.roomId);
  const asset = assets.find((item) => item.id === task.assetId);
  return (
    <article className={`task-card state-${state}`} aria-label={`${task.title}, ${state}`}>
      <div className="task-main">
        <span className={`status-mark ${state}`} aria-hidden="true">{state === 'overdue' ? <AlertTriangle /> : state === 'on-demand' ? <PackageOpen /> : <CalendarClock />}</span>
        <div className="task-copy">
          <div className="task-kicker"><span>{task.category}</span><span className={`priority ${task.priority}`}>{task.priority}</span></div>
          <h3>{task.title}</h3>
          <div className="task-meta">
            <span><Clock3 aria-hidden="true" />{task.dueDate ? state === 'today' ? 'Today' : task.dueDate : 'On demand'}</span>
            <span><UserRound aria-hidden="true" />{APP_CONFIG.displayNames[task.assignedTo]}</span>
            {(asset || room) && <span><MapPin aria-hidden="true" />{asset?.name || room?.name}</span>}
          </div>
        </div>
        <button className="complete-button" type="button" onClick={() => onAction(task, 'complete')} aria-label={`Complete ${task.title}`}><Check aria-hidden="true" /><span>Done</span></button>
      </div>
      <div className="task-actions" aria-label={`Actions for ${task.title}`}>
        <button type="button" onClick={() => onAction(task, 'snooze')}>Snooze</button>
        <button type="button" onClick={() => onAction(task, 'skip')}>Skip</button>
        <button type="button" onClick={() => onHistory(task)}><History aria-hidden="true" /> History</button>
        <button type="button" onClick={() => onEdit(task)} aria-label={`Edit ${task.title}`}><MoreHorizontal aria-hidden="true" /> Edit</button>
      </div>
    </article>
  );
}
