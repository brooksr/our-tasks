import type { Asset, HouseholdSnapshot, MaintenanceTask, RecurrenceConfig, Room, Supply } from './types';

const now = '2026-07-12T15:00:00.000Z';
const base = { active: true, createdAt: now, updatedAt: now, version: 1 };

const rooms: Room[] = [
  { ...base, id: 'room-kitchen', name: 'Kitchen', floor: '1', zone: 'interior', unityObjectId: 'Kitchen' },
  { ...base, id: 'room-laundry', name: 'Laundry', floor: '1', zone: 'interior', unityObjectId: 'Laundry' },
  { ...base, id: 'room-backyard', name: 'Backyard', floor: 'outside', zone: 'yard', unityObjectId: 'Backyard' },
  { ...base, id: 'room-garage', name: 'Garage', floor: '1', zone: 'garage', unityObjectId: 'Garage' },
  { ...base, id: 'room-whole-home', name: 'Whole home', floor: 'all', zone: 'house', unityObjectId: 'HouseRoot' }
];

const assets: Asset[] = [
  { ...base, id: 'asset-truck', name: '2022 Ford F-150', type: 'vehicle', category: 'Vehicle', roomId: 'room-garage', unityObjectId: 'DrivewayVehicle', manufacturer: 'Ford', model: 'F-150', notes: 'Seed intervals are suggestions; follow the owner manual and actual usage.' },
  { ...base, id: 'asset-hot-tub', name: 'Hot tub', type: 'spa', category: 'Hot Tub', roomId: 'room-backyard', unityObjectId: 'BackyardHotTub' },
  { ...base, id: 'asset-hvac', name: 'HVAC system', type: 'appliance', category: 'Appliances', roomId: 'room-whole-home', unityObjectId: 'HVAC' },
  { ...base, id: 'asset-dishwasher', name: 'Dishwasher', type: 'appliance', category: 'Appliances', roomId: 'room-kitchen', unityObjectId: 'Dishwasher' },
  { ...base, id: 'asset-dryer', name: 'Dryer', type: 'appliance', category: 'Appliances', roomId: 'room-laundry', unityObjectId: 'Dryer' }
];

type SeedTask = Partial<MaintenanceTask> & Pick<MaintenanceTask, 'id' | 'title' | 'category' | 'recurrenceType'>;
function task(seed: SeedTask): MaintenanceTask {
  return {
    ...base, description: '', locationIds: [], unityObjectIds: [], assignedTo: 'either', assignmentMode: 'not_last',
    priority: 'normal', status: 'open', recurrenceConfig: {}, nextDateStrategy: 'scheduled_date', seasonalRegion: 'Thousand Oaks, CA',
    defaultSnoozeDays: 3, requiresReading: false, requiresMileage: false, requiresUsageCount: false, createdBy: 'primary',
    ...seed
  };
}
const interval = (interval: number, unit: RecurrenceConfig['unit']): RecurrenceConfig => ({ interval, unit });

const tasks: MaintenanceTask[] = [
  task({ id: 'task-litter-scoop', title: 'Scoop kitty litter', category: 'Pets', dueDate: '2026-07-12', recurrenceType: 'interval', recurrenceConfig: interval(1, 'days'), estimatedMinutes: 5 }),
  task({ id: 'task-litter-replace', title: 'Fully replace kitty litter', category: 'Pets', dueDate: '2026-07-14', recurrenceType: 'interval', recurrenceConfig: interval(14, 'days'), nextDateStrategy: 'completion_date' }),
  task({ id: 'task-litter-deep', title: 'Deep-clean litter box', category: 'Pets', dueDate: '2026-07-18', recurrenceType: 'interval', recurrenceConfig: interval(30, 'days'), nextDateStrategy: 'completion_date' }),
  task({ id: 'task-mileage', title: 'Check vehicle mileage', category: 'Vehicle', assetId: 'asset-truck', roomId: 'room-garage', dueDate: '2026-07-10', recurrenceType: 'monthly', recurrenceConfig: interval(1, 'months'), requiresMileage: true, priority: 'high' }),
  task({ id: 'task-oil', title: 'Change vehicle oil', category: 'Vehicle', assetId: 'asset-truck', roomId: 'room-garage', dueDate: '2026-10-01', recurrenceType: 'mileage', recurrenceConfig: { mileageInterval: 7500, calendarFallback: { interval: 12, unit: 'months' } }, requiresMileage: true }),
  task({ id: 'task-tires', title: 'Check tire pressure', category: 'Vehicle', assetId: 'asset-truck', roomId: 'room-garage', dueDate: '2026-08-01', recurrenceType: 'monthly', recurrenceConfig: interval(1, 'months') }),
  task({ id: 'task-plants-water', title: 'Water indoor plants', category: 'Plants', dueDate: '2026-07-13', dueWindowStart: '2026-07-11', dueWindowEnd: '2026-07-15', recurrenceType: 'interval', recurrenceConfig: interval(7, 'days'), nextDateStrategy: 'completion_date' }),
  task({ id: 'task-irrigation', title: 'Inspect irrigation for leaks', category: 'Yard', roomId: 'room-backyard', dueDate: '2026-07-15', recurrenceType: 'monthly', recurrenceConfig: interval(1, 'months') }),
  task({ id: 'task-hot-tub-test', title: 'Test hot-tub water chemistry', category: 'Hot Tub', assetId: 'asset-hot-tub', roomId: 'room-backyard', dueDate: '2026-07-12', recurrenceType: 'weekly', recurrenceConfig: { weekdays: [3, 6] }, requiresReading: true, unityObjectIds: ['BackyardHotTub'] }),
  task({ id: 'task-hot-tub-filter', title: 'Rinse hot-tub filter', category: 'Hot Tub', assetId: 'asset-hot-tub', roomId: 'room-backyard', dueDate: '2026-07-20', recurrenceType: 'interval', recurrenceConfig: interval(14, 'days'), unityObjectIds: ['BackyardHotTub'] }),
  task({ id: 'task-front-weeds', title: 'Weed front yard', category: 'Yard', dueDate: '2026-07-16', recurrenceType: 'interval', recurrenceConfig: interval(14, 'days'), nextDateStrategy: 'completion_date' }),
  task({ id: 'task-defensible', title: 'Review defensible space', category: 'Safety', roomId: 'room-backyard', dueDate: '2026-07-19', recurrenceType: 'interval', recurrenceConfig: interval(3, 'months'), priority: 'high' }),
  task({ id: 'task-dry-vegetation', title: 'Clear dry vegetation', category: 'Seasonal', roomId: 'room-backyard', dueDate: '2026-07-21', recurrenceType: 'seasonal', recurrenceConfig: { months: [5, 6, 7, 8, 9, 10] }, priority: 'high' }),
  task({ id: 'task-hvac-filter', title: 'Replace HVAC air filter', category: 'Appliances', assetId: 'asset-hvac', roomId: 'room-whole-home', dueDate: '2026-07-11', recurrenceType: 'interval', recurrenceConfig: interval(90, 'days'), priority: 'high', unityObjectIds: ['HVAC'] }),
  task({ id: 'task-smoke', title: 'Test smoke detectors', category: 'Safety', roomId: 'room-whole-home', dueDate: '2026-07-25', recurrenceType: 'interval', recurrenceConfig: interval(6, 'months'), priority: 'high' }),
  task({ id: 'task-dishwasher', title: 'Clean dishwasher filter', category: 'Appliances', assetId: 'asset-dishwasher', roomId: 'room-kitchen', dueDate: '2026-07-17', recurrenceType: 'monthly', recurrenceConfig: interval(1, 'months') }),
  task({ id: 'task-dryer', title: 'Clean dryer lint path', category: 'Appliances', assetId: 'asset-dryer', roomId: 'room-laundry', dueDate: '2026-07-22', recurrenceType: 'interval', recurrenceConfig: interval(3, 'months') }),
  task({ id: 'task-emergency', title: 'Review home emergency supplies', category: 'Safety', roomId: 'room-whole-home', dueDate: '2026-08-01', recurrenceType: 'interval', recurrenceConfig: interval(6, 'months') })
];

const supplySeeds: Array<[string, string, string, number, number]> = [
  ['kitty-litter', 'Kitty litter', 'Pets', 1, 1], ['hvac-filter', 'HVAC filters', 'Appliances', 0, 1],
  ['fridge-filter', 'Refrigerator water filters', 'Appliances', 1, 1], ['hot-tub-sanitizer', 'Hot-tub sanitizer', 'Hot Tub', 2, 1],
  ['ph-up', 'pH increaser', 'Hot Tub', 1, 1], ['ph-down', 'pH decreaser', 'Hot Tub', 1, 1],
  ['alkalinity', 'Alkalinity increaser', 'Hot Tub', 1, 1], ['filter-cleaner', 'Hot-tub filter cleaner', 'Hot Tub', 0.5, 0.5],
  ['plant-food', 'Plant fertilizer', 'Plants', 1, 0.5], ['weed-supplies', 'Weed-removal supplies', 'Yard', 1, 0.5],
  ['vehicle-cleaner', 'Vehicle cleaning supplies', 'Vehicle', 1, 0.5]
];
const supplies: Supply[] = supplySeeds.map(([id, name, category, quantity, threshold]) => ({
  ...base, id: `supply-${id}`, name, category, unit: 'unit', quantity, reorderThreshold: threshold, reorderQuantity: 1
}));

export function createSeedSnapshot(): HouseholdSnapshot {
  return {
    users: [
      { ...base, id: 'primary', email: 'demo-primary@local', displayName: 'Brooks', role: 'admin' },
      { ...base, id: 'secondary', email: 'demo-secondary@local', displayName: 'Wife', role: 'member' },
      { ...base, id: 'household', email: 'demo-household@local', displayName: 'Household', role: 'member' }
    ],
    rooms, assets, tasks, supplies,
    events: [{ id: 'event-seed', taskId: 'task-litter-scoop', eventType: 'completed', eventDate: '2026-07-11T16:00:00.000Z', performedBy: 'secondary', previousDueDate: '2026-07-11', nextDueDate: '2026-07-12', notes: 'All done.', createdAt: '2026-07-11T16:00:00.000Z' }]
  };
}

export const CATEGORIES = ['Pets', 'Vehicle', 'House', 'Yard', 'Plants', 'Hot Tub', 'Cleaning', 'Appliances', 'Safety', 'Supplies', 'Smart Home', 'Seasonal', 'Other'];
