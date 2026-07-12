# Our Tasks

Our Tasks is a shared household-maintenance system wrapped around the existing
Unity home model. It is mobile-first, installable as a PWA, useful without
WebGL, and ready to use with local demo data before Google is configured.

The original product brief is preserved in [docs/PRODUCT_SPEC.md](docs/PRODUCT_SPEC.md).

## Architecture

- React and TypeScript provide the dashboard, task actions, history, supplies,
  filters, authentication, sync state, and responsive mobile UI.
- IndexedDB stores a complete local snapshot and a durable offline action queue.
- Google Identity Services supplies an ID token in configured mode.
- Google Apps Script verifies that token and the two-account allowlist on every
  request, then reads and writes Google Sheets with optimistic locking.
- Unity 6000.5.2f1 and the built-in render pipeline provide the 3D house. The
  current build is lazy-loaded under `public/unity/house`; new maintenance C#
  components live in `UnityProject/Assets/Scripts/Maintenance`.
- The service worker precaches only the small app shell. Unity files are cached
  on demand in a separately versioned runtime cache.

## Local development

Requires Node 20 or newer.

```bash
npm install
npm run dev
```

With no environment file, the app clearly runs in local demo mode. Data is
stored in the browser's `our-tasks-v1` IndexedDB database. To reset it, clear
site data in browser developer tools.

Useful checks:

```bash
npm test
npm run build
npm run lint
```

## Runtime configuration

Copy `.env.example` to `.env.local`. All runtime configuration is centralized
in `src/config.ts`; environment variables only replace its placeholders.

| Variable | Purpose |
| --- | --- |
| `VITE_BASE_PATH` | GitHub Pages repository path, for example `/our-tasks/` |
| `VITE_GOOGLE_CLIENT_ID` | Google OAuth web client ID |
| `VITE_PRIMARY_EMAIL` / `VITE_SECONDARY_EMAIL` | Exactly two approved accounts |
| `VITE_APPS_SCRIPT_ENDPOINT` | Deployed Apps Script `/exec` URL |
| `VITE_APP_URL` | Canonical deployed app URL used in emails |
| `VITE_BABY_APP_URL` | Optional link to the separate BabySteps app |
| `VITE_MOCK_MODE` | `true` locally; CI sets `false` |

Names such as “Brooks” and “Wife” are display labels and can be changed in
`src/config.ts` without committing real account names.

## Google setup

Follow [docs/SETUP_GOOGLE.md](docs/SETUP_GOOGLE.md). In summary:

1. Create the OAuth web client and authorize the local and Pages origins.
2. Create an Apps Script project, copy `apps-script/`, and configure Script
   Properties for the spreadsheet, OAuth client, two emails, and app URL.
3. Run `setupDatabase()`, optionally `seedRecommendedData()`, and
   `installReminderTriggers()`.
4. Deploy as a web app executing as the owner, with access set to anyone. The
   backend still rejects any request without a valid token for an allowed user.
5. Add the matching GitHub Actions repository variables.

No Google client secret, private key, or smart-home credential belongs in this
repository or in Google Sheets.

## Unity and WebGL

Open `UnityProject` in Unity 6000.5.2f1. The imported source retains the existing
`Main` and `HouseModel_ScannedPlan` scenes, `CommerceShell` WebGL template, house
controls, materials, and generated scan assets. The added maintenance code does
not replace those systems.

Before rebuilding:

1. Add `HomeAsset` to selectable objects and enter stable `assetId`, `roomId`,
   and `unityObjectId` values that match Google Sheets.
2. Run **Tools → Our Tasks → Validate asset mappings**.
3. Build the house scene to `public/unity/house` or build through the existing
   house WebGL builder and copy its output there.
4. Confirm the host in `public/unity/house/index.html` still includes the
   maintenance `postMessage` listener after regenerating the template.

The checked-in WebGL binary was rebuilt with the new C# bridge. Spatial status
and object-selection callbacks become visible as `HomeAsset` mappings are added
to scene objects. Protocol details are in
[docs/UNITY_BRIDGE.md](docs/UNITY_BRIDGE.md).

## GitHub Pages

The workflow in `.github/workflows/deploy.yml` tests and builds on pushes to
`main`, then uploads `dist`. Enable Pages with **GitHub Actions** as its source
and create the repository variables listed above. Vite, the service worker,
manifest, and Unity frame all use repository-relative paths.

The Unity build is about 70 MB. It is excluded from installation precache and
downloaded only when the user opens House view. The browser may retain those
files in the `our-tasks-runtime-v1` cache after the first visit.

## Reminders and optional integrations

- Apps Script supports daily summaries at 8:00 AM and weekly summaries Sunday
  at 8:00 AM in `America/Los_Angeles`.
- Calendar events are opt-in per task via the Apps Script foundation; the app
  never creates an event for every task automatically.
- Browser notification support is only a progressive enhancement.
- Smart-home commands use `ISmartHomeProvider`; only an unavailable mock is
  included. Do not put device credentials in the browser, Sheets, or Unity.
- BabySteps integration is intentionally limited to a future summary/link
  surface. This application stores no detailed baby data.

## Troubleshooting

- **Local demo appears in production:** all four Google variables must be set;
  placeholder detection is intentionally strict.
- **Forbidden after Google login:** the browser variables and Apps Script
  `ALLOWED_EMAILS` must match, lower/upper case aside.
- **Invalid token:** `GOOGLE_CLIENT_ID` in Script Properties must exactly equal
  the browser client ID.
- **Schema missing:** run `setupDatabase()` as the Apps Script owner.
- **Changes stay queued:** open Settings, inspect sync state, restore network,
  then select Sync now. Queued actions are never silently discarded.
- **Unity stays blank:** serve the app over HTTP rather than `file://`, check the
  browser console, and verify the `Build/house-build.*` paths.

## Security limitations

GitHub Pages is public static hosting. Login protects normal UI access and Apps
Script protects all household records, but static Unity files cannot be made
private by client-side authentication: someone with their exact URL can fetch
them. The copied build therefore has no credentials or exact street-address
label, but it does contain a representation of a private residence. For strong
layout confidentiality, host the Unity artifacts behind an authenticated CDN
or application server and set a restrictive CSP there. Do not rely on an
unlisted Pages URL as access control.

The recommended production CSP should allow this origin, Google Identity
Services, the Apps Script endpoint, and WebAssembly while continuing to block
`eval`, unknown frames, and arbitrary scripts. Apps Script sanitizes strings,
validates HTTPS links and JSON fields, verifies token audience and email, uses
soft deletion, and returns conflicts instead of overwriting newer versions.

## Documentation

- [Implementation plan](docs/IMPLEMENTATION_PLAN.md)
- [Data model](docs/DATA_MODEL.md)
- [Recurrence rules](docs/RECURRENCE.md)
- [Unity bridge](docs/UNITY_BRIDGE.md)
- [Google setup](docs/SETUP_GOOGLE.md)
