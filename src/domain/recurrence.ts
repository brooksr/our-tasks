import type { MaintenanceTask, RecurrenceConfig } from './types';

const DAY_MS = 86_400_000;

function parseDate(value: string) {
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day, 12));
}

export function dateKey(date: Date) {
  return date.toISOString().slice(0, 10);
}

function addMonths(date: Date, amount: number) {
  const day = date.getUTCDate();
  const result = new Date(date);
  result.setUTCDate(1);
  result.setUTCMonth(result.getUTCMonth() + amount);
  const last = new Date(Date.UTC(result.getUTCFullYear(), result.getUTCMonth() + 1, 0, 12)).getUTCDate();
  result.setUTCDate(Math.min(day, last));
  return result;
}

function addInterval(date: Date, config: RecurrenceConfig) {
  const interval = Math.max(1, config.interval ?? 1);
  const result = new Date(date);
  if (config.unit === 'weeks') result.setUTCDate(result.getUTCDate() + interval * 7);
  else if (config.unit === 'months') return addMonths(result, interval);
  else if (config.unit === 'years') return addMonths(result, interval * 12);
  else result.setUTCDate(result.getUTCDate() + interval);
  return result;
}

function nextWeekly(base: Date, config: RecurrenceConfig) {
  const weekdays = [...(config.weekdays?.length ? config.weekdays : [base.getUTCDay()])].sort();
  const interval = Math.max(1, config.interval ?? 1);
  for (let days = 1; days <= interval * 14 + 7; days += 1) {
    const candidate = new Date(base.getTime() + days * DAY_MS);
    if (!weekdays.includes(candidate.getUTCDay())) continue;
    if (interval === 1) return candidate;
    const weeks = Math.floor(days / 7);
    if (weeks >= interval - 1) return candidate;
  }
  return new Date(base.getTime() + interval * 7 * DAY_MS);
}

function ordinalWeekday(year: number, month: number, weekday: number, ordinal: number) {
  if (ordinal === -1) {
    const last = new Date(Date.UTC(year, month + 1, 0, 12));
    last.setUTCDate(last.getUTCDate() - ((last.getUTCDay() - weekday + 7) % 7));
    return last;
  }
  const first = new Date(Date.UTC(year, month, 1, 12));
  const day = 1 + ((weekday - first.getUTCDay() + 7) % 7) + (ordinal - 1) * 7;
  return new Date(Date.UTC(year, month, day, 12));
}

function nextMonthly(base: Date, config: RecurrenceConfig) {
  const interval = Math.max(1, config.interval ?? 1);
  const targetMonth = addMonths(new Date(Date.UTC(base.getUTCFullYear(), base.getUTCMonth(), 1, 12)), interval);
  if (config.weekdayOrdinal && config.weekday !== undefined) {
    return ordinalWeekday(targetMonth.getUTCFullYear(), targetMonth.getUTCMonth(), config.weekday, config.weekdayOrdinal);
  }
  const lastDay = new Date(Date.UTC(targetMonth.getUTCFullYear(), targetMonth.getUTCMonth() + 1, 0, 12)).getUTCDate();
  const wanted = config.dayOfMonth === 'last' ? lastDay : (config.dayOfMonth ?? base.getUTCDate());
  targetMonth.setUTCDate(Math.min(Number(wanted), lastDay));
  return targetMonth;
}

function nextAnnual(base: Date, config: RecurrenceConfig) {
  const dates = config.annualDates?.length ? config.annualDates : [dateKey(base).slice(5)];
  const candidates: Date[] = [];
  for (let year = base.getUTCFullYear(); year <= base.getUTCFullYear() + 2; year += 1) {
    dates.forEach((monthDay) => candidates.push(parseDate(`${year}-${monthDay}`)));
  }
  return candidates.sort((a, b) => a.getTime() - b.getTime()).find((date) => date > base) ?? addMonths(base, 12);
}

export function calculateNextDueDate(task: MaintenanceTask, completedAt = dateKey(new Date()), scheduledDate = task.dueDate) {
  if (task.recurrenceType === 'none' || task.recurrenceType === 'on_demand' || task.recurrenceType === 'threshold') return undefined;
  const anchorKey = task.nextDateStrategy === 'scheduled_date' && scheduledDate ? scheduledDate : completedAt;
  const base = parseDate(anchorKey);
  const config = task.recurrenceConfig;
  let result: Date;
  switch (task.recurrenceType) {
    case 'weekly': result = nextWeekly(base, config); break;
    case 'monthly': result = nextMonthly(base, config); break;
    case 'annual': result = nextAnnual(base, config); break;
    case 'seasonal': {
      const months = [...(config.months ?? [2, 5, 8, 10])].sort((a, b) => a - b);
      const candidate = months.map((month) => new Date(Date.UTC(base.getUTCFullYear(), month - 1, 1, 12))).find((date) => date > base);
      result = candidate ?? new Date(Date.UTC(base.getUTCFullYear() + 1, months[0] - 1, 1, 12));
      break;
    }
    case 'mileage':
    case 'usage': {
      const fallback = config.calendarFallback;
      result = fallback ? addInterval(base, fallback) : addMonths(base, 12);
      break;
    }
    default: result = addInterval(base, config);
  }
  return dateKey(result);
}

export function dueState(task: MaintenanceTask, today = dateKey(new Date())) {
  if (!task.dueDate) return 'on-demand' as const;
  if (task.dueDate < today) return 'overdue' as const;
  if (task.dueDate === today) return 'today' as const;
  const soon = dateKey(new Date(parseDate(today).getTime() + 7 * DAY_MS));
  return task.dueDate <= soon ? 'soon' as const : 'upcoming' as const;
}

export function evaluateCounterTrigger(task: MaintenanceTask, value: number) {
  if (task.recurrenceType === 'mileage') return value >= (task.recurrenceConfig.mileageInterval ?? Infinity);
  if (task.recurrenceType === 'usage') return value >= (task.recurrenceConfig.usageInterval ?? Infinity);
  return false;
}
