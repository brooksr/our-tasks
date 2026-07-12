# Google setup

This setup has no client secret. The browser receives a public OAuth client ID;
Apps Script verifies Google ID tokens and the two-email allowlist before it
touches Sheets.

## 1. OAuth client

1. In Google Cloud Console, create/select a project and configure the OAuth
   consent screen for the two household testers.
2. Create an **OAuth client ID → Web application**.
3. Add authorized JavaScript origins, without paths:
   `http://localhost:5173` and `https://YOUR_USER.github.io`.
4. Copy the client ID. Do not create or commit a client secret for this app.

## 2. Apps Script and Sheets

1. Create a standalone Apps Script project at script.google.com.
2. Copy the files in `apps-script/` into it, including the manifest (enable
   **Show appsscript.json** in Project Settings).
3. In **Project Settings → Script Properties**, add:
   - `GOOGLE_CLIENT_ID`: the exact web client ID.
   - `ALLOWED_EMAILS`: exactly two comma-separated approved Google addresses.
   - `APP_URL`: the final Pages URL.
4. Run `setupDatabase()`. It creates a spreadsheet when `SPREADSHEET_ID` is
   absent, stores the new ID, and creates/migrates every required tab/header.
   Alternatively set `SPREADSHEET_ID` first to use an empty existing Sheet.
5. Optionally run `seedRecommendedData()`. The rich browser demo seed can be
   used as a guide for adding editable household tasks after first connection.

The script owner will be prompted to authorize Sheets, external token
verification, email, triggers, and optional Calendar access.

## 3. Deploy the API

1. Select **Deploy → New deployment → Web app**.
2. Execute as **Me** (the household database owner).
3. Set access to **Anyone**. This makes the endpoint reachable, not the data
   public: `authorize()` still requires a valid ID token for this client and one
   of the two allowed emails on every POST.
4. Deploy and copy the URL ending in `/exec`.
5. After code changes, create a new deployment version or edit the active
   deployment. A `/dev` URL works only for script editors and is unsuitable for
   the household app.

## 4. Configure local and Pages builds

Create `.env.local` from `.env.example` and set client ID, emails, endpoint, app
URL, and an appropriate base path (`/` for ordinary local Vite development).

In GitHub repository **Settings → Secrets and variables → Actions → Variables**,
add:

- `VITE_GOOGLE_CLIENT_ID`
- `VITE_PRIMARY_EMAIL`
- `VITE_SECONDARY_EMAIL`
- `VITE_APPS_SCRIPT_ENDPOINT`
- `VITE_APP_URL`
- optional `VITE_BABY_APP_URL`

The workflow supplies the repository base path automatically. Enable Pages with
GitHub Actions as its source.

## 5. Reminders

Run `installReminderTriggers()` once. It removes old Our Tasks reminder triggers
before installing daily 8:00 AM and Sunday 8:00 AM triggers in
`America/Los_Angeles`. Toggle `dailyEmailEnabled` and `weeklyEmailEnabled` in
Settings. Apps Script time triggers may run within Google's normal scheduling
window rather than at the exact minute.

Calendar reminders are opt-in: `createCalendarReminder(taskId, calendarId)` is
the backend foundation. Do not call it automatically for every task.

## Verification

1. Open the deployed app with each approved account; both should load the same
   snapshot and be able to mutate tasks.
2. Open it with a third account; the API response must be `FORBIDDEN`.
3. Change a row's version, then submit an older edit; the API must return
   `VERSION_CONFLICT` with local/server details.
4. Disable the network, perform a snooze, reload, and confirm the action and
   pending count remain. Restore the network and sync.
5. Inspect the repository and Sheet to confirm no tokens, secrets, smart-home
   credentials, or private keys were stored.

## CSP guidance

For a host that permits response headers, start with `default-src 'self'` and
allow `script-src`/`frame-src` for `https://accounts.google.com`, `connect-src`
for the deployed Apps Script/Google endpoints, and WebAssembly needed by Unity.
Keep `object-src 'none'`, `base-uri 'self'`, and `frame-ancestors 'self'`. GitHub
Pages cannot set arbitrary security headers, which is another reason to use an
authenticated host if the 3D layout requires strong confidentiality.
