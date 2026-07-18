/* global ContentService, PropertiesService, SpreadsheetApp, UrlFetchApp, Utilities, CacheService, LockService */

const TZ = 'America/Los_Angeles';
const DEFAULT_SPREADSHEET_ID = '1lY8G7YZl3n3xvwTWpp1A4z-50gdH2D96Q6jD9bJxCDs';
const JSON_FIELDS = ['locationIds', 'unityObjectIds', 'recurrenceConfig', 'capabilities', 'readings', 'suppliesUsed', 'attachmentUrls'];
const SOFT_DELETE = ['Rooms', 'Assets', 'Tasks', 'Supplies', 'Shopping', 'DeviceMappings'];
const SCHEMAS = {
  Users: ['id','email','displayName','role','active','createdAt','updatedAt','version'],
  Rooms: ['id','name','floor','zone','unityObjectId','notes','active','createdAt','updatedAt','version','deletedAt'],
  Assets: ['id','name','type','category','roomId','locationId','unityObjectId','smartDeviceId','smartHomeProvider','manufacturer','model','serialNumber','purchaseDate','warrantyExpiration','manualUrl','photoUrl','notes','active','createdAt','updatedAt','version','deletedAt'],
  Tasks: ['id','title','description','category','roomId','assetId','locationIds','unityObjectIds','assignedTo','assignmentMode','priority','status','dueDate','dueWindowStart','dueWindowEnd','recurrenceType','recurrenceConfig','nextDateStrategy','seasonalRegion','estimatedMinutes','defaultSnoozeDays','requiresReading','requiresMileage','requiresUsageCount','active','createdBy','createdAt','updatedAt','version','deletedAt'],
  TaskEvents: ['id','taskId','eventType','eventDate','performedBy','previousDueDate','nextDueDate','notes','reason','cost','mileage','usageCount','timeSpentMinutes','readings','suppliesUsed','attachmentUrls','createdAt'],
  Supplies: ['id','name','category','assetId','unit','quantity','reorderThreshold','reorderQuantity','preferredProductUrl','notes','active','createdAt','updatedAt','version','deletedAt'],
  Shopping: ['id','name','category','checked','addedBy','note','active','createdAt','updatedAt','version','deletedAt'],
  TaskSupplies: ['id','taskId','supplyId','defaultQuantityUsed','required','createdAt','updatedAt'],
  DeviceMappings: ['id','assetId','unityObjectId','provider','deviceId','deviceType','capabilities','enabled','createdAt','updatedAt','version','active','deletedAt'],
  Settings: ['key','value','description','updatedAt','updatedBy']
};
const ENTITY_MAP = { users:'Users', rooms:'Rooms', assets:'Assets', tasks:'Tasks', events:'TaskEvents', supplies:'Supplies', shopping:'Shopping', taskSupplies:'TaskSupplies', deviceMappings:'DeviceMappings', settings:'Settings' };

function doGet() {
  return output({ success:true, data:{ service:'our-tasks', status:'ok' }, error:null, serverTime:isoNow() });
}

function doPost(event) {
  try {
    const request = JSON.parse((event && event.postData && event.postData.contents) || '{}');
    const user = authorize(request.idToken);
    const data = route(request.action, sanitize(request.payload || {}), user);
    return output({ success:true, data:data, error:null, serverTime:isoNow() });
  } catch (error) {
    const code = error.code || 'SERVER_ERROR';
    return output({ success:false, data:null, error:{ code:code, message:error.message || String(error), details:error.details || null }, serverTime:isoNow() });
  }
}

function route(action, payload, user) {
  if (action === 'bootstrap') return bootstrap();
  if (action === 'task.action') return taskAction(payload, user);
  const match = /^(users|rooms|assets|tasks|events|supplies|shopping|taskSupplies|deviceMappings|settings)\.(list|get|upsert|delete)$/.exec(action || '');
  if (!match) fail('UNKNOWN_ACTION', 'Unknown API action.');
  const sheetName = ENTITY_MAP[match[1]];
  if (sheetName === 'TaskEvents' && (match[2] === 'upsert' || match[2] === 'delete')) fail('IMMUTABLE_HISTORY', 'Task events are append-only.');
  if (match[2] === 'list') return readAll(sheetName, payload.includeDeleted === true);
  if (match[2] === 'get') return getById(sheetName, payload.id);
  if (match[2] === 'upsert') return upsert(sheetName, payload, user.email);
  return remove(sheetName, payload, user.email);
}

function authorize(idToken) {
  if (!idToken) fail('UNAUTHENTICATED', 'A Google identity token is required.');
  const cache = CacheService.getScriptCache();
  const cached = cache.get('token:' + idToken.slice(-32));
  let claims;
  if (cached) claims = JSON.parse(cached);
  else {
    const response = UrlFetchApp.fetch('https://oauth2.googleapis.com/tokeninfo?id_token=' + encodeURIComponent(idToken), { muteHttpExceptions:true });
    if (response.getResponseCode() !== 200) fail('INVALID_TOKEN', 'Google could not verify this identity token.');
    claims = JSON.parse(response.getContentText());
    cache.put('token:' + idToken.slice(-32), JSON.stringify(claims), 300);
  }
  const props = PropertiesService.getScriptProperties();
  const clientId = props.getProperty('GOOGLE_CLIENT_ID');
  if (!clientId || claims.aud !== clientId || claims.email_verified !== 'true') fail('INVALID_TOKEN', 'The token is not valid for this application.');
  const allowed = authorizedEmails();
  if (!allowed.length) fail('SERVER_CONFIG', 'Add at least one active email to the Users sheet or ALLOWED_EMAILS Script Property.');
  if (allowed.indexOf(String(claims.email).toLowerCase()) < 0) fail('FORBIDDEN', 'This Google account is not approved for the household.');
  return { email:String(claims.email).toLowerCase(), name:claims.name || claims.email };
}

function authorizedEmails() {
  let allowed = [];
  try {
    allowed = readAll('Users', true).filter(function(user){ return user.active !== false && user.email; }).map(function(user){ return String(user.email).trim().toLowerCase(); });
  } catch (_) {
    // During first-time setup the Users sheet may not exist yet.
  }
  if (!allowed.length) {
    allowed = (PropertiesService.getScriptProperties().getProperty('ALLOWED_EMAILS') || '').split(',').map(function(value){ return value.trim().toLowerCase(); }).filter(Boolean);
  }
  return allowed.filter(function(email, index){ return allowed.indexOf(email) === index; });
}

function bootstrap() {
  return {
    users:readAll('Users'), rooms:readAll('Rooms'), assets:readAll('Assets'), tasks:readAll('Tasks'),
    events:readAll('TaskEvents'), supplies:readAll('Supplies'), shopping:readAll('Shopping'), taskSupplies:readAll('TaskSupplies'),
    deviceMappings:readAll('DeviceMappings'), settings:readAll('Settings')
  };
}

function taskAction(input, user) {
  const lock = LockService.getScriptLock();
  lock.waitLock(15000);
  try {
    const task = getById('Tasks', input.taskId);
    if (!task) fail('NOT_FOUND', 'Task not found.');
    if (Number(task.version) !== Number(input.expectedVersion)) failConflict(input, task);
    const before = task.dueDate;
    const eventType = { complete:'completed', snooze:'snoozed', skip:'skipped', archive:'archived', reassign:'assigned' }[input.action];
    if (!eventType) fail('VALIDATION', 'Invalid task action.');
    if (input.action === 'complete') {
      task.dueDate = input.nextDueDate || '';
      task.status = task.dueDate || task.recurrenceType === 'on_demand' ? 'open' : 'completed';
    } else if (input.action === 'snooze') {
      task.dueDate = input.nextDueDate || addDaysDateKey(Number(input.snoozeDays || task.defaultSnoozeDays || 3));
      task.status = 'snoozed';
    } else if (input.action === 'skip') {
      task.dueDate = input.nextDueDate || task.dueDate;
      task.status = 'open';
    } else if (input.action === 'archive') {
      task.status = 'archived'; task.active = false; task.deletedAt = isoNow();
    } else if (input.action === 'reassign') task.assignedTo = input.assignedTo;
    const saved = upsert('Tasks', Object.assign({}, task, { expectedVersion:Number(input.expectedVersion) }), user.email);
    const event = {
      id:Utilities.getUuid(), taskId:task.id, eventType:eventType, eventDate:isoNow(), performedBy:user.email,
      previousDueDate:before || '', nextDueDate:saved.dueDate || '', notes:input.notes || '', reason:input.reason || '',
      cost:input.cost || '', mileage:input.mileage || '', usageCount:input.usageCount || '', timeSpentMinutes:input.timeSpentMinutes || '',
      readings:input.readings || {}, suppliesUsed:input.suppliesUsed || [], attachmentUrls:input.attachmentUrls || [], createdAt:isoNow()
    };
    appendEvent(event);
    if (input.action === 'complete' && Array.isArray(input.suppliesUsed)) deductSupplies(input.suppliesUsed, user.email);
    return { task:saved, event:event };
  } finally { lock.releaseLock(); }
}

function appendEvent(event) {
  const sheet = requireSheet('TaskEvents');
  sheet.appendRow(toRow('TaskEvents', event));
}

function deductSupplies(usage, email) {
  usage.forEach(function(item) {
    const supply = getById('Supplies', item.supplyId);
    if (!supply) return;
    supply.quantity = Math.max(0, Number(supply.quantity || 0) - Math.max(0, Number(item.quantity || 0)));
    upsert('Supplies', Object.assign({}, supply, { expectedVersion:Number(supply.version) }), email);
  });
}

function upsert(sheetName, value, email) {
  validateRecord(sheetName, value);
  const sheet = requireSheet(sheetName);
  const headers = SCHEMAS[sheetName];
  const rows = sheet.getDataRange().getValues();
  const key = sheetName === 'Settings' ? 'key' : 'id';
  const keyIndex = headers.indexOf(key);
  let rowNumber = -1;
  for (let i=1; i<rows.length; i++) if (String(rows[i][keyIndex]) === String(value[key])) { rowNumber = i + 1; break; }
  const timestamp = isoNow();
  if (rowNumber > 0) {
    const current = fromRow(sheetName, rows[rowNumber - 1]);
    const expected = value.expectedVersion === undefined ? value.version : value.expectedVersion;
    if (headers.indexOf('version') >= 0 && Number(expected) !== Number(current.version)) failConflict(value, current);
    value = Object.assign({}, current, value, { updatedAt:timestamp, version:Number(current.version || 0) + 1 });
    sheet.getRange(rowNumber, 1, 1, headers.length).setValues([toRow(sheetName, value)]);
  } else {
    if (!value[key]) value[key] = Utilities.getUuid();
    value = Object.assign({ active:true, createdAt:timestamp, version:1 }, value, { updatedAt:timestamp });
    sheet.appendRow(toRow(sheetName, value));
  }
  return value;
}

function remove(sheetName, input, email) {
  const record = getById(sheetName, input.id);
  if (!record) fail('NOT_FOUND', 'Record not found.');
  if (SOFT_DELETE.indexOf(sheetName) >= 0) return upsert(sheetName, Object.assign({}, record, { active:false, deletedAt:isoNow(), expectedVersion:input.expectedVersion }), email);
  fail('DELETE_NOT_ALLOWED', 'This record type cannot be deleted.');
}

function readAll(sheetName, includeDeleted) {
  const sheet = requireSheet(sheetName);
  const values = sheet.getDataRange().getValues();
  if (values.length < 2) return [];
  return values.slice(1).filter(function(row){ return row.some(function(cell){ return cell !== ''; }); }).map(function(row){ return fromRow(sheetName, row); }).filter(function(record){ return includeDeleted || !record.deletedAt; });
}

function getById(sheetName, id) {
  const key = sheetName === 'Settings' ? 'key' : 'id';
  return readAll(sheetName, true).filter(function(record){ return String(record[key]) === String(id); })[0] || null;
}

function requireSheet(name) {
  const id = PropertiesService.getScriptProperties().getProperty('SPREADSHEET_ID') || DEFAULT_SPREADSHEET_ID;
  if (!id) fail('SERVER_CONFIG', 'Set SPREADSHEET_ID in Script Properties.');
  const sheet = SpreadsheetApp.openById(id).getSheetByName(name);
  if (!sheet) fail('SCHEMA_MISSING', 'Run setupDatabase() to create the ' + name + ' sheet.');
  return sheet;
}

function fromRow(sheetName, row) {
  const result = {};
  SCHEMAS[sheetName].forEach(function(header, index) {
    let value = row[index];
    if (JSON_FIELDS.indexOf(header) >= 0) { try { value = value ? JSON.parse(value) : (header === 'recurrenceConfig' || header === 'readings' ? {} : []); } catch (_) { value = header === 'recurrenceConfig' ? {} : []; } }
    result[header] = value;
  });
  return result;
}

function toRow(sheetName, record) {
  return SCHEMAS[sheetName].map(function(header) {
    const value = record[header];
    return JSON_FIELDS.indexOf(header) >= 0 ? JSON.stringify(value === undefined ? (header === 'recurrenceConfig' ? {} : []) : value) : value === undefined ? '' : value;
  });
}

function validateRecord(sheetName, value) {
  if (!value || typeof value !== 'object') fail('VALIDATION', 'A record object is required.');
  ['manualUrl','photoUrl','preferredProductUrl'].forEach(function(field) {
    if (value[field] && !/^https:\/\//i.test(String(value[field]))) fail('VALIDATION', field + ' must use HTTPS.');
  });
  JSON_FIELDS.forEach(function(field) {
    if (value[field] !== undefined) { try { JSON.stringify(value[field]); } catch (_) { fail('INVALID_JSON', field + ' is not valid JSON.'); } }
  });
  if (sheetName === 'Tasks' && value.recurrenceType && ['none','on_demand','interval','weekly','monthly','annual','seasonal','mileage','usage','threshold'].indexOf(value.recurrenceType) < 0) fail('VALIDATION', 'Unsupported recurrence type.');
}

function sanitize(value) {
  if (typeof value === 'string') return value.replace(/[<>]/g, '').trim().slice(0, 10000);
  if (Array.isArray(value)) return value.slice(0, 1000).map(sanitize);
  if (value && typeof value === 'object') { const clean = {}; Object.keys(value).slice(0, 200).forEach(function(key){ clean[key.replace(/[^A-Za-z0-9_]/g, '')] = sanitize(value[key]); }); return clean; }
  return value;
}

function output(value) { return ContentService.createTextOutput(JSON.stringify(value)).setMimeType(ContentService.MimeType.JSON); }
function isoNow() { return Utilities.formatDate(new Date(), TZ, "yyyy-MM-dd'T'HH:mm:ssXXX"); }
function addDaysDateKey(days) { const date = new Date(); date.setDate(date.getDate() + days); return Utilities.formatDate(date, TZ, 'yyyy-MM-dd'); }
function fail(code, message, details) { const error = new Error(message); error.code = code; error.details = details; throw error; }
function failConflict(local, server) { fail('VERSION_CONFLICT', 'This record changed on another device.', { local:local, server:server }); }
