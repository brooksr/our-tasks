/* global SCHEMAS, SpreadsheetApp, PropertiesService, Utilities, isoNow */

function setupDatabase() {
  const props = PropertiesService.getScriptProperties();
  let id = props.getProperty('SPREADSHEET_ID');
  const spreadsheet = id ? SpreadsheetApp.openById(id) : SpreadsheetApp.create('Our Tasks household database');
  if (!id) { id = spreadsheet.getId(); props.setProperty('SPREADSHEET_ID', id); }
  Object.keys(SCHEMAS).forEach(function(name) {
    let sheet = spreadsheet.getSheetByName(name);
    if (!sheet) sheet = spreadsheet.insertSheet(name);
    const headers = SCHEMAS[name];
    const existing = sheet.getLastColumn() ? sheet.getRange(1, 1, 1, sheet.getLastColumn()).getValues()[0] : [];
    headers.forEach(function(header) { if (existing.indexOf(header) < 0) existing.push(header); });
    sheet.getRange(1, 1, 1, existing.length).setValues([existing]);
    sheet.setFrozenRows(1);
    sheet.getRange(1, 1, 1, existing.length).setFontWeight('bold').setBackground('#396b58').setFontColor('#ffffff');
  });
  const defaultSheet = spreadsheet.getSheetByName('Sheet1');
  if (defaultSheet && spreadsheet.getSheets().length > 1) spreadsheet.deleteSheet(defaultSheet);
  setSetting('schemaVersion', '1', 'Current database schema version', 'setup');
  return spreadsheet.getUrl();
}

function migrateDatabase() { return setupDatabase(); }

function configureProject(spreadsheetId, googleClientId, primaryEmail, secondaryEmail, appUrl) {
  PropertiesService.getScriptProperties().setProperties({
    SPREADSHEET_ID:spreadsheetId, GOOGLE_CLIENT_ID:googleClientId,
    ALLOWED_EMAILS:[primaryEmail, secondaryEmail].join(','), APP_URL:appUrl
  }, false);
  return setupDatabase();
}

function seedRecommendedData() {
  const timestamp = isoNow();
  const users = (PropertiesService.getScriptProperties().getProperty('ALLOWED_EMAILS') || '').split(',');
  const userSheet = SpreadsheetApp.openById(PropertiesService.getScriptProperties().getProperty('SPREADSHEET_ID')).getSheetByName('Users');
  if (userSheet.getLastRow() < 2) {
    [['primary',users[0] || 'REPLACE_WITH_PRIMARY_EMAIL','Brooks','admin'],['secondary',users[1] || 'REPLACE_WITH_SECONDARY_EMAIL','Wife','member']].forEach(function(row){ userSheet.appendRow(row.concat([true,timestamp,timestamp,1])); });
  }
  const roomSheet = SpreadsheetApp.openById(PropertiesService.getScriptProperties().getProperty('SPREADSHEET_ID')).getSheetByName('Rooms');
  if (roomSheet.getLastRow() < 2) [['room-kitchen','Kitchen','1','interior','Kitchen'],['room-backyard','Backyard','outside','yard','Backyard'],['room-garage','Garage','1','garage','Garage'],['room-whole-home','Whole home','all','house','HouseRoot']].forEach(function(row){ roomSheet.appendRow(row.concat(['',true,timestamp,timestamp,1,''])); });
  if (readAll('Assets').length === 0) {
    [
      { id:'asset-truck', name:'2022 Ford F-150', type:'vehicle', category:'Vehicle', roomId:'room-garage', unityObjectId:'DrivewayVehicle', manufacturer:'Ford', model:'F-150', notes:'Follow manufacturer documentation and actual usage.' },
      { id:'asset-hot-tub', name:'Hot tub', type:'spa', category:'Hot Tub', roomId:'room-backyard', unityObjectId:'BackyardHotTub' },
      { id:'asset-hvac', name:'HVAC system', type:'appliance', category:'Appliances', roomId:'room-whole-home', unityObjectId:'HVAC' }
    ].forEach(function(asset){ upsert('Assets', asset, 'setup'); });
  }
  if (readAll('Tasks').length === 0) {
    [
      seedTask('litter-scoop','Scoop kitty litter','Pets',0,'interval',{interval:1,unit:'days'}),
      seedTask('litter-replace','Fully replace kitty litter','Pets',2,'interval',{interval:14,unit:'days'},'completion_date'),
      seedTask('litter-deep','Deep-clean litter box','Pets',6,'interval',{interval:30,unit:'days'},'completion_date'),
      seedTask('mileage','Check vehicle mileage','Vehicle',0,'monthly',{interval:1,unit:'months'},'scheduled_date','asset-truck','room-garage',true),
      seedTask('oil','Change vehicle oil','Vehicle',80,'mileage',{mileageInterval:7500,calendarFallback:{interval:12,unit:'months'}},'scheduled_date','asset-truck','room-garage',true),
      seedTask('plants-water','Water indoor plants','Plants',1,'interval',{interval:7,unit:'days'},'completion_date'),
      seedTask('irrigation','Inspect irrigation for leaks','Yard',3,'monthly',{interval:1,unit:'months'},'scheduled_date','', 'room-backyard'),
      seedTask('hot-tub-test','Test hot-tub water chemistry','Hot Tub',0,'weekly',{weekdays:[3,6]},'scheduled_date','asset-hot-tub','room-backyard'),
      seedTask('hot-tub-filter','Rinse hot-tub filter','Hot Tub',8,'interval',{interval:14,unit:'days'},'scheduled_date','asset-hot-tub','room-backyard'),
      seedTask('front-weeds','Weed front yard','Yard',4,'interval',{interval:14,unit:'days'},'completion_date'),
      seedTask('defensible','Review defensible space','Safety',7,'interval',{interval:3,unit:'months'}),
      seedTask('dry-vegetation','Clear dry vegetation','Seasonal',9,'seasonal',{months:[5,6,7,8,9,10]}),
      seedTask('hvac-filter','Replace HVAC air filter','Appliances',0,'interval',{interval:90,unit:'days'},'scheduled_date','asset-hvac','room-whole-home'),
      seedTask('smoke','Test smoke detectors','Safety',13,'interval',{interval:6,unit:'months'}),
      seedTask('dishwasher','Clean dishwasher filter','Appliances',5,'monthly',{interval:1,unit:'months'}),
      seedTask('dryer','Clean dryer lint path','Appliances',10,'interval',{interval:3,unit:'months'}),
      seedTask('emergency','Review home emergency supplies','Safety',20,'interval',{interval:6,unit:'months'})
    ].forEach(function(task){ upsert('Tasks', task, 'setup'); });
  }
  if (readAll('Supplies').length === 0) {
    [['kitty-litter','Kitty litter','Pets',1,1],['hvac-filter','HVAC filters','Appliances',0,1],['fridge-filter','Refrigerator water filters','Appliances',1,1],['hot-tub-sanitizer','Hot-tub sanitizer','Hot Tub',2,1],['ph-up','pH increaser','Hot Tub',1,1],['ph-down','pH decreaser','Hot Tub',1,1],['alkalinity','Alkalinity increaser','Hot Tub',1,1],['filter-cleaner','Hot-tub filter cleaner','Hot Tub',0.5,0.5],['plant-food','Plant fertilizer','Plants',1,0.5],['weed-supplies','Weed-removal supplies','Yard',1,0.5],['vehicle-cleaner','Vehicle cleaning supplies','Vehicle',1,0.5]].forEach(function(item){ upsert('Supplies', { id:'supply-' + item[0], name:item[1], category:item[2], unit:'unit', quantity:item[3], reorderThreshold:item[4], reorderQuantity:1 }, 'setup'); });
  }
  setSetting('dailyEmailEnabled', 'true', 'Enable the daily household summary', 'setup');
  setSetting('weeklyEmailEnabled', 'true', 'Enable the weekly household summary', 'setup');
  setSetting('appUrl', PropertiesService.getScriptProperties().getProperty('APP_URL') || '', 'GitHub Pages application URL', 'setup');
}

function seedTask(id, title, category, dueOffset, recurrenceType, recurrenceConfig, strategy, assetId, roomId, requiresMileage) {
  const due = new Date(); due.setDate(due.getDate() + dueOffset);
  return {
    id:'task-' + id, title:title, description:'Editable recommended starting interval.', category:category,
    roomId:roomId || '', assetId:assetId || '', locationIds:[], unityObjectIds:[], assignedTo:'either', assignmentMode:'not_last',
    priority:category === 'Safety' ? 'high' : 'normal', status:'open', dueDate:Utilities.formatDate(due, 'America/Los_Angeles', 'yyyy-MM-dd'),
    recurrenceType:recurrenceType, recurrenceConfig:recurrenceConfig, nextDateStrategy:strategy || 'scheduled_date', seasonalRegion:'Thousand Oaks, CA',
    defaultSnoozeDays:3, requiresReading:title.indexOf('chemistry') >= 0, requiresMileage:requiresMileage === true, requiresUsageCount:false,
    active:true, createdBy:'primary'
  };
}

function setSetting(key, value, description, updatedBy) {
  const sheet = SpreadsheetApp.openById(PropertiesService.getScriptProperties().getProperty('SPREADSHEET_ID')).getSheetByName('Settings');
  const rows = sheet.getDataRange().getValues();
  for (let i=1; i<rows.length; i++) if (rows[i][0] === key) { sheet.getRange(i+1,1,1,5).setValues([[key,value,description,isoNow(),updatedBy]]); return; }
  sheet.appendRow([key,value,description,isoNow(),updatedBy]);
}
