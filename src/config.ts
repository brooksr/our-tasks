export const AUTH_CONFIG = {
  googleClientId: import.meta.env.VITE_GOOGLE_CLIENT_ID || 'REPLACE_WITH_GOOGLE_CLIENT_ID'
};

export const APP_CONFIG = {
  appName: 'Our Tasks',
  appUrl: import.meta.env.VITE_APP_URL || window.location.origin + import.meta.env.BASE_URL,
  appsScriptEndpoint: import.meta.env.VITE_APPS_SCRIPT_ENDPOINT || 'REPLACE_WITH_APPS_SCRIPT_URL',
  spreadsheetId: '1lY8G7YZl3n3xvwTWpp1A4z-50gdH2D96Q6jD9bJxCDs',
  timezone: 'America/Los_Angeles',
  dailyReminderHour: 8,
  weeklyReminderDay: 0,
  displayNames: { primary: 'Brooks', secondary: 'Wife', either: 'Either', shared: 'Shared' },
  mockMode: import.meta.env.VITE_MOCK_MODE !== 'false',
  unity: { enabled: true, buildPath: `${import.meta.env.BASE_URL}unity/house/index.html`, lazyLoad: true },
  features: { calendar: false, browserNotifications: true, babySummary: true },
  smartHomeProvider: 'mock',
  babyAppUrl: import.meta.env.VITE_BABY_APP_URL || ''
};

export function isGoogleConfigured() {
  return !AUTH_CONFIG.googleClientId.startsWith('REPLACE_') &&
    !APP_CONFIG.appsScriptEndpoint.startsWith('REPLACE_');
}
