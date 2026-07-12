# Implementation plan

## Repository assessment

The target repository initially contained only the product brief. There was no
application, Unity project, deployment workflow, or repository-local guidance
to preserve. Two sibling projects were inspected as implementation references:

- `unity-commerce` is the source of the current house experience. It uses Unity
  `6000.5.2f1`, the built-in render pipeline, the scenes `Main` and
  `HouseModel_ScannedPlan`, and the custom `CommerceShell` WebGL template. The
  house scene already exposes camera, floor, shell, cutaway, scan, explode, and
  reset controls through `HouseModelController`. Its standalone WebGL build is
  uncompressed and approximately 70 MB.
- `babysteps` is a Vite 5 / React 18 / TypeScript application. It demonstrates
  a mobile-first shell, Dexie-backed IndexedDB storage, Google Identity Services
  OAuth, Google Sheets access, a service worker, Vitest, and GitHub Pages
  deployment. Baby-specific data and behavior will not be copied.

The house reference worktree contains uncommitted changes. This project treats
it as read-only and copies only the current build/source artifacts needed for
integration, without changing that worktree.

## Decisions

1. Use a React/TypeScript overlay as the primary maintenance interface and keep
   the house inside a lazy-loaded iframe. This avoids loading a large Unity
   build on every mobile visit and keeps the task list usable without WebGL.
2. Keep all runtime configuration in `src/config.ts`. Placeholder OAuth values
   select an explicit local demo mode.
3. Put recurrence and assignment logic in dependency-free domain modules so it
   can be tested independently from React and Unity.
4. Use IndexedDB for the local cache and action queue. Adapters implement one
   repository contract; the UI does not depend on the selected backend.
5. Use an Apps Script web app as the only production write path. Google Sheets
   is not called directly from the browser. The backend verifies the Google ID
   token and checks the allowlist on every request.
6. Copy the existing house WebGL output under `public/unity/house` and exclude
   it from aggressive service-worker precaching. Add a source-level Unity
   maintenance bridge and stable asset mapping components for the next rebuild.
7. Remove exact street-address labels from the user-facing host UI and docs in
   this repository. The private 3D layout remains gated by the application login
   in configured mode; GitHub Pages itself must still be treated as public
   static hosting.

## Implementation sequence

1. Scaffold Vite, React, TypeScript, linting, testing, PWA assets, and Pages CI.
2. Implement models, seed data, recurrence, assignment, task actions, and
   supply deductions with unit tests.
3. Implement the dashboard, filters, detail/action dialogs, history, supplies,
   house view, settings, accessibility, responsive navigation, and theme.
4. Implement IndexedDB cache/queue, mock and Apps Script adapters, Google
   sign-in, conflict reporting, retry, and sync status.
5. Add the Apps Script schema, CRUD/action routes, authorization, optimistic
   locking, reminders, calendar hooks, setup, migration, and seed functions.
6. Integrate the existing house build, add the JavaScript bridge, and add Unity
   source components/editor validation without replacing the reference scene.
7. Document setup, data model, recurrence, bridge, security limitations, and
   deployment; then run tests, production build, static checks, and browser QA.

## Known constraints

- OAuth credentials, the two allowed email addresses, and the deployed Apps
  Script URL must be supplied by the household owner.
- A rebuilt Unity binary is required before newly added C# maintenance behavior
  is present inside the copied WebGL player. Until then, the bridge fails safely
  and the house remains viewable with its current controls.
- The Unity binary is intentionally runtime-cached rather than precached because
  of its size. Offline task data remains available even if the house is not.
