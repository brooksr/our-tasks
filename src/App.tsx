import { AlertCircle, Boxes, CheckCircle2, ChevronRight, ClipboardCheck, History, Home, House, ListFilter, Moon, Package, Plus, RefreshCw, Search, Settings, Sun, UserRound, WifiOff, X } from 'lucide-react';
import { useCallback, useEffect, useMemo, useState } from 'react';
import { ActionDialog } from './components/ActionDialog';
import { HouseView } from './components/HouseView';
import { TaskCard } from './components/TaskCard';
import { TaskEditorDialog } from './components/TaskEditorDialog';
import { GoogleSignInButton } from './components/GoogleSignInButton';
import { APP_CONFIG, isGoogleConfigured } from './config';
import { CATEGORIES } from './domain/seed';
import { dueState } from './domain/recurrence';
import type { HouseholdSnapshot, MaintenanceTask, RepositoryStatus, TaskActionInput } from './domain/types';
import { getGoogleCredential } from './storage/googleAuth';
import { householdRepository } from './storage';

type View = 'home' | 'tasks' | 'house' | 'supplies' | 'settings';
const views: Array<{ id: View; label: string; icon: typeof Home }> = [
  { id: 'home', label: 'Home', icon: Home }, { id: 'tasks', label: 'Tasks', icon: ClipboardCheck },
  { id: 'house', label: 'House', icon: House }, { id: 'supplies', label: 'Supplies', icon: Package },
  { id: 'settings', label: 'Settings', icon: Settings }
];

function syncIcon(status: RepositoryStatus) {
  if (status.state === 'offline') return <WifiOff aria-hidden="true" />;
  if (status.state === 'error' || status.state === 'conflict') return <AlertCircle aria-hidden="true" />;
  return status.state === 'syncing' ? <RefreshCw className="spin" aria-hidden="true" /> : <CheckCircle2 aria-hidden="true" />;
}

function App() {
  const [snapshot, setSnapshot] = useState<HouseholdSnapshot | null>(null);
  const [view, setView] = useState<View>('home');
  const [status, setStatus] = useState(householdRepository.getStatus());
  const [signedIn, setSignedIn] = useState(!isGoogleConfigured() || Boolean(getGoogleCredential()));
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [theme, setTheme] = useState<'light' | 'dark'>(() => (localStorage.getItem('our-tasks.theme') as 'light' | 'dark') || 'light');
  const [query, setQuery] = useState('');
  const [category, setCategory] = useState('All');
  const [person, setPerson] = useState('All');
  const [action, setAction] = useState<{ task: MaintenanceTask; type: 'complete' | 'snooze' | 'skip' } | null>(null);
  const [historyTask, setHistoryTask] = useState<MaintenanceTask | null>(null);
  const [editingTask, setEditingTask] = useState<MaintenanceTask | 'new' | null>(null);
  const [updateReady, setUpdateReady] = useState(false);

  const refresh = useCallback(async () => {
    const data = await householdRepository.initialize(); setSnapshot(data); setStatus(householdRepository.getStatus());
  }, []);
  useEffect(() => householdRepository.subscribe(setStatus), []);
  useEffect(() => {
    document.documentElement.dataset.theme = theme; localStorage.setItem('our-tasks.theme', theme);
  }, [theme]);
  useEffect(() => {
    const online = () => householdRepository.sync().then(setSnapshot).catch(() => undefined);
    window.addEventListener('online', online);
    const update = () => setUpdateReady(true); window.addEventListener('app-update-ready', update);
    return () => { window.removeEventListener('online', online); window.removeEventListener('app-update-ready', update); };
  }, []);
  useEffect(() => {
    if (!signedIn) { setLoading(false); return; }
    refresh().catch((caught) => setError(caught instanceof Error ? caught.message : 'Unable to open household data.')).finally(() => setLoading(false));
  }, [refresh, signedIn]);

  const googleSignIn = useCallback(async () => {
    setLoading(true); setError('');
    try { const data = await householdRepository.sync(); setSnapshot(data); setSignedIn(true); }
    catch (caught) { setSignedIn(false); setError(caught instanceof Error ? caught.message : 'Sign-in failed.'); }
    finally { setLoading(false); }
  }, []);

  const googleSignInError = useCallback((caught: Error) => {
    setLoading(false); setError(caught.message || 'Sign-in failed.');
  }, []);

  async function performAction(values: Omit<TaskActionInput, 'taskId' | 'performedBy' | 'expectedVersion' | 'action'>) {
    if (!action) return;
    try {
      const data = await householdRepository.performAction({
        ...values, taskId: action.task.id, action: action.type, performedBy: 'primary', expectedVersion: action.task.version
      });
      setSnapshot(data); setAction(null);
    } catch (caught) { setError(caught instanceof Error ? caught.message : 'Unable to save the task action.'); }
  }

  async function saveTask(task: MaintenanceTask) {
    try { await householdRepository.saveTask(task); setSnapshot(await householdRepository.getSnapshot()); setEditingTask(null); }
    catch (caught) { setError(caught instanceof Error ? caught.message : 'Unable to save the task.'); }
  }

  const tasks = useMemo(() => {
    if (!snapshot) return [];
    const needle = query.trim().toLowerCase();
    return snapshot.tasks.filter((task) => task.active && task.status !== 'archived')
      .filter((task) => category === 'All' || task.category === category)
      .filter((task) => person === 'All' || task.assignedTo === person)
      .filter((task) => !needle || `${task.title} ${task.description} ${task.category} ${snapshot.assets.find((a) => a.id === task.assetId)?.name || ''} ${snapshot.rooms.find((r) => r.id === task.roomId)?.name || ''}`.toLowerCase().includes(needle))
      .sort((a, b) => (a.dueDate || '9999').localeCompare(b.dueDate || '9999'));
  }, [snapshot, category, person, query]);

  if (!signedIn) return <main className="login-page"><section className="login-card"><div className="brand-mark"><House aria-hidden="true" /></div><span className="eyebrow">Private household space</span><h1>Care for the place<br />that cares for you.</h1><p>Shared maintenance, supplies, and a task-aware view of home.</p>{error && <p className="error-banner" role="alert">{error}</p>}<GoogleSignInButton disabled={loading} onError={googleSignInError} onSignedIn={googleSignIn} />{loading && <p className="signin-progress">Opening your household…</p>}<small>Access is limited to active household accounts in the Users sheet.</small></section></main>;
  if (loading || !snapshot) return <main className="loading-page"><div className="brand-mark"><House aria-hidden="true" /></div><RefreshCw className="spin" aria-hidden="true" /><p>Opening your household…</p></main>;

  const overdue = tasks.filter((task) => dueState(task) === 'overdue');
  const today = tasks.filter((task) => dueState(task) === 'today');
  const soon = tasks.filter((task) => dueState(task) === 'soon');
  const recent = snapshot.events.filter((event) => event.eventType === 'completed').slice(0, 4);
  const lowSupplies = snapshot.supplies.filter((supply) => supply.active && supply.quantity <= supply.reorderThreshold);
  const countFor = (who: 'primary' | 'secondary') => snapshot.tasks.filter((task) => task.active && task.assignedTo === who).length;

  const taskList = (items: MaintenanceTask[]) => items.map((task) => <TaskCard key={task.id} task={task} assets={snapshot.assets} rooms={snapshot.rooms} onAction={(selected, type) => setAction({ task: selected, type })} onHistory={setHistoryTask} onEdit={setEditingTask} />);

  return (
    <div className="app-shell">
      <header className="topbar"><button className="wordmark" type="button" onClick={() => setView('home')}><span className="brand-mark small"><House aria-hidden="true" /></span><span>Our Tasks</span></button><div className="header-actions"><button className={`sync-pill ${status.state}`} type="button" onClick={() => householdRepository.sync().then(setSnapshot).catch(() => undefined)}>{syncIcon(status)}<span>{status.state}{status.pending ? ` · ${status.pending}` : ''}</span></button><button className="icon-button" type="button" onClick={() => setTheme(theme === 'light' ? 'dark' : 'light')} aria-label={`Use ${theme === 'light' ? 'dark' : 'light'} mode`}>{theme === 'light' ? <Moon aria-hidden="true" /> : <Sun aria-hidden="true" />}</button><span className="avatar" aria-label="Signed in as Brooks">B</span></div></header>
      {error && <div className="error-banner global" role="alert"><AlertCircle aria-hidden="true" /><span>{error}</span><button onClick={() => setError('')} aria-label="Dismiss"><X aria-hidden="true" /></button></div>}
      {updateReady && <div className="update-banner">A new version is ready.<button onClick={() => location.reload()}>Refresh</button></div>}

      <main className="main-content">
        {view === 'home' && <>
          <section className="hero"><div><span className="eyebrow">Sunday, July 12</span><h1>Good morning, Brooks.</h1><p>{overdue.length ? `${overdue.length} overdue ${overdue.length === 1 ? 'task needs' : 'tasks need'} attention.` : 'Everything urgent is handled.'}</p></div><button className="house-shortcut" type="button" onClick={() => setView('house')}><span><House aria-hidden="true" /></span><strong>Open house view</strong><small>See tasks in place</small><ChevronRight aria-hidden="true" /></button></section>
          <section className="summary-grid" aria-label="Household summary"><article className="summary-card urgent"><span>Overdue</span><strong>{overdue.length}</strong><small>{overdue[0]?.title || 'Nothing overdue'}</small></article><article className="summary-card"><span>Due today</span><strong>{today.length}</strong><small>{today[0]?.title || 'A clear day'}</small></article><article className="summary-card"><span>This week</span><strong>{soon.length}</strong><small>{soon.length ? 'Coming up soon' : 'Nothing scheduled'}</small></article><article className="summary-card"><span>Low supplies</span><strong>{lowSupplies.length}</strong><small>{lowSupplies[0]?.name || 'Stock looks good'}</small></article></section>
          <div className="dashboard-grid"><div className="dashboard-main">
            {overdue.length > 0 && <section className="task-section"><div className="section-heading"><div><span className="section-dot urgent" /><h2>Needs attention</h2></div><span>{overdue.length}</span></div>{taskList(overdue.slice(0, 4))}</section>}
            <section className="task-section"><div className="section-heading"><div><span className="section-dot today" /><h2>Today</h2></div><span>{today.length}</span></div>{today.length ? taskList(today) : <div className="empty-state"><CheckCircle2 aria-hidden="true" /><p>No more tasks due today.</p></div>}</section>
            {soon.length > 0 && <section className="task-section"><div className="section-heading"><div><span className="section-dot soon" /><h2>Later this week</h2></div><button onClick={() => setView('tasks')}>View all</button></div>{taskList(soon.slice(0, 3))}</section>}
          </div><aside className="dashboard-aside">
            <section className="aside-card"><div className="section-heading"><div><Boxes aria-hidden="true" /><h2>Low supplies</h2></div></div>{lowSupplies.length ? lowSupplies.map((supply) => <div className="supply-row" key={supply.id}><span>{supply.name}<small>{supply.quantity} {supply.unit} left</small></span><span className="low-badge">Low</span></div>) : <p className="muted">Everything is stocked.</p>}<button className="text-button" onClick={() => setView('supplies')}>Manage supplies <ChevronRight aria-hidden="true" /></button></section>
            <section className="aside-card"><div className="section-heading"><div><UserRound aria-hidden="true" /><h2>Assignment balance</h2></div></div><div className="workload"><div><span>Brooks</span><strong>{countFor('primary')}</strong></div><div className="workload-bar"><i style={{ width: `${Math.max(8, countFor('primary') / Math.max(1, countFor('primary') + countFor('secondary')) * 100)}%` }} /></div><div><span>Wife</span><strong>{countFor('secondary')}</strong></div></div></section>
            <section className="aside-card"><div className="section-heading"><div><History aria-hidden="true" /><h2>Recently completed</h2></div></div>{recent.map((event) => <button className="recent-row" key={event.id} onClick={() => setHistoryTask(snapshot.tasks.find((task) => task.id === event.taskId) || null)}><CheckCircle2 aria-hidden="true" /><span>{snapshot.tasks.find((task) => task.id === event.taskId)?.title || 'Task'}<small>{new Date(event.eventDate).toLocaleDateString()}</small></span></button>)}</section>
          </aside></div>
        </>}

        {view === 'tasks' && <section className="page-view"><div className="page-heading"><div><span className="eyebrow">Shared household</span><h1>Tasks</h1><p>{tasks.length} open tasks across the home.</p></div><button className="primary" type="button" onClick={() => setEditingTask('new')}><Plus aria-hidden="true" /> New task</button></div><div className="filter-bar"><label className="search-field"><Search aria-hidden="true" /><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Search tasks, rooms, assets…" /></label><label><ListFilter aria-hidden="true" /><select value={category} onChange={(e) => setCategory(e.target.value)}><option>All</option>{CATEGORIES.map((item) => <option key={item}>{item}</option>)}</select></label><label><UserRound aria-hidden="true" /><select value={person} onChange={(e) => setPerson(e.target.value)}><option>All</option><option value="primary">Brooks</option><option value="secondary">Wife</option><option value="either">Either</option><option value="shared">Shared</option></select></label></div><div className="task-section">{taskList(tasks)}</div></section>}
        {view === 'house' && <section className="page-view house-page"><div className="page-heading"><div><span className="eyebrow">Maintenance mode</span><h1>House view</h1><p>Select rooms and assets in the 3D home.</p></div></div><HouseView tasks={tasks} onSelection={(selection) => { const matched = tasks.find((task) => task.assetId === selection.assetId || task.roomId === selection.roomId); if (matched) setHistoryTask(matched); }} /></section>}
        {view === 'supplies' && <section className="page-view"><div className="page-heading"><div><span className="eyebrow">Household stock</span><h1>Supplies</h1><p>Completing tasks can deduct the amount used.</p></div></div><div className="supply-grid">{snapshot.supplies.map((supply) => <article className="supply-card" key={supply.id}><div><span className="category-icon"><Package aria-hidden="true" /></span>{supply.quantity <= supply.reorderThreshold && <span className="low-badge">Low stock</span>}</div><h2>{supply.name}</h2><strong>{supply.quantity} <small>{supply.unit}</small></strong><div className="stock-meter"><i style={{ width: `${Math.min(100, supply.quantity / Math.max(1, supply.reorderThreshold * 2) * 100)}%` }} /></div><p>Reorder at {supply.reorderThreshold} · Buy {supply.reorderQuantity}</p></article>)}</div></section>}
        {view === 'settings' && <section className="page-view settings-view"><div className="page-heading"><div><span className="eyebrow">Configuration</span><h1>Settings</h1><p>Household data, reminders, and integrations.</p></div></div><article className="settings-card"><h2>Data connection</h2><dl><div><dt>Mode</dt><dd>{isGoogleConfigured() ? 'Google Apps Script + Sheets' : 'Local demo'}</dd></div><div><dt>Sync</dt><dd>{status.state}{status.pending ? ` (${status.pending} queued)` : ''}</dd></div><div><dt>Timezone</dt><dd>{APP_CONFIG.timezone}</dd></div></dl><button className="secondary" onClick={() => householdRepository.sync().then(setSnapshot)}>Sync now</button></article><article className="settings-card"><h2>Reminders</h2><p>Daily summaries are scheduled for 8:00 AM. Weekly household summaries run Sunday at 8:00 AM after Apps Script triggers are installed.</p></article><article className="settings-card"><h2>BabySteps</h2><p>Only a future summary-card interface is reserved here. No baby care or health data is stored in this app.</p>{APP_CONFIG.babyAppUrl && <a className="secondary button-link" href={APP_CONFIG.babyAppUrl}>Open BabySteps</a>}</article></section>}
      </main>

      <nav className="bottom-nav" aria-label="Primary navigation">{views.map((item) => { const Icon = item.icon; return <button type="button" className={view === item.id ? 'active' : ''} key={item.id} onClick={() => setView(item.id)} aria-current={view === item.id ? 'page' : undefined}><Icon aria-hidden="true" /><span>{item.label}</span></button>; })}</nav>
      {action && <ActionDialog task={action.task} action={action.type} supplies={snapshot.supplies} onClose={() => setAction(null)} onSubmit={performAction} />}
      {editingTask && <TaskEditorDialog task={editingTask === 'new' ? undefined : editingTask} rooms={snapshot.rooms} assets={snapshot.assets} onClose={() => setEditingTask(null)} onSave={saveTask} />}
      {historyTask && <dialog open className="history-dialog" aria-labelledby="history-title"><div className="dialog-head"><div><span className="eyebrow">History</span><h2 id="history-title">{historyTask.title}</h2></div><button className="icon-button" onClick={() => setHistoryTask(null)} aria-label="Close"><X aria-hidden="true" /></button></div><ol className="timeline">{snapshot.events.filter((event) => event.taskId === historyTask.id).sort((a, b) => b.eventDate.localeCompare(a.eventDate)).map((event) => <li key={event.id}><span className="timeline-mark"><CheckCircle2 aria-hidden="true" /></span><div><strong>{event.eventType}</strong><small>{new Date(event.eventDate).toLocaleString()} · {APP_CONFIG.displayNames[event.performedBy as keyof typeof APP_CONFIG.displayNames] || event.performedBy}</small>{event.notes && <p>{event.notes}</p>}{(event.previousDueDate || event.nextDueDate) && <p className="date-change">{event.previousDueDate || 'No date'} → {event.nextDueDate || 'No next date'}</p>}</div></li>)}</ol></dialog>}
    </div>
  );
}

export default App;
