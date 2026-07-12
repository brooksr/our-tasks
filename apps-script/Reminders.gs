/* global PropertiesService, MailApp, ScriptApp, readAll, getById, Utilities, TZ */

function installReminderTriggers() {
  ScriptApp.getProjectTriggers().forEach(function(trigger) {
    if (['sendDailySummary','sendWeeklySummary'].indexOf(trigger.getHandlerFunction()) >= 0) ScriptApp.deleteTrigger(trigger);
  });
  ScriptApp.newTrigger('sendDailySummary').timeBased().atHour(8).everyDays(1).inTimezone(TZ).create();
  ScriptApp.newTrigger('sendWeeklySummary').timeBased().onWeekDay(ScriptApp.WeekDay.SUNDAY).atHour(8).everyWeeks(1).inTimezone(TZ).create();
}

function sendDailySummary() { sendSummary(false); }
function sendWeeklySummary() { sendSummary(true); }

function sendSummary(weekly) {
  const settings = {};
  readAll('Settings').forEach(function(item){ settings[item.key] = item.value; });
  if (String(settings[weekly ? 'weeklyEmailEnabled' : 'dailyEmailEnabled']) === 'false') return;
  const today = Utilities.formatDate(new Date(), TZ, 'yyyy-MM-dd');
  const weekEndDate = new Date(); weekEndDate.setDate(weekEndDate.getDate() + 7);
  const weekEnd = Utilities.formatDate(weekEndDate, TZ, 'yyyy-MM-dd');
  const tasks = readAll('Tasks').filter(function(task){ return task.active && task.status !== 'archived'; });
  const supplies = readAll('Supplies').filter(function(supply){ return supply.active && Number(supply.quantity) <= Number(supply.reorderThreshold); });
  readAll('Users').filter(function(user){ return user.active; }).forEach(function(user) {
    const relevant = tasks.filter(function(task){ return ['either','shared',user.id].indexOf(task.assignedTo) >= 0 && task.dueDate && task.dueDate <= weekEnd; });
    const lines = relevant.map(function(task){ return (task.dueDate < today ? 'OVERDUE — ' : '') + task.title + ' (' + task.dueDate + ')'; });
    const supplyLines = supplies.map(function(supply){ return supply.name + ': ' + supply.quantity + ' ' + supply.unit; });
    const subject = weekly ? 'Weekly household summary' : 'Today at home';
    const body = [subject, '', lines.length ? lines.join('\n') : 'No assigned tasks due in the next seven days.', '', 'Low supplies:', supplyLines.length ? supplyLines.join('\n') : 'None', '', settings.appUrl || PropertiesService.getScriptProperties().getProperty('APP_URL') || ''].join('\n');
    MailApp.sendEmail({ to:user.email, subject:subject, body:body, name:'Our Tasks' });
  });
}

function createCalendarReminder(taskId, calendarId) {
  const task = getById('Tasks', taskId);
  if (!task || !task.dueDate) throw new Error('The task needs a due date.');
  const calendar = CalendarApp.getCalendarById(calendarId);
  return calendar.createAllDayEvent(task.title, new Date(task.dueDate + 'T12:00:00')).getId();
}
