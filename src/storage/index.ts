import { isGoogleConfigured } from '../config';
import { AppsScriptHouseholdRepository } from './appsScriptRepository';
import { LocalHouseholdRepository } from './localRepository';

export const householdRepository = isGoogleConfigured()
  ? new AppsScriptHouseholdRepository()
  : new LocalHouseholdRepository();
