You are working inside an existing Unity-based smart-home project that contains a 3D duplicate of a private residence.

Your task is to add a complete household maintenance and recurring-task system to the existing application.

Do not create a separate standalone application. Integrate the maintenance system into the existing Unity smart-home control app while preserving all current scenes, controls, assets, project settings, and behavior.

Primary objective
-----------------

Create a shared household-maintenance module that:

*   Runs in the existing Unity WebGL application.
    
*   Is deployable as a static site on GitHub Pages.
    
*   Uses Google authentication.
    
*   Restricts access to two explicitly approved Google accounts.
    
*   Uses Google Sheets as the primary database.
    
*   Uses Google Apps Script as the authenticated backend API.
    
*   Supports both household members equally.
    
*   Integrates maintenance tasks with rooms, physical assets, Unity objects, and future smart-home controls.
    
*   Works with mock local data before Google services are configured.
    
*   Is mobile-first and usable from Android.
    
*   Is installable as a PWA where practical.
    
*   Supports future integration with the separate baby-tracking application, but does not duplicate baby-tracking functionality.
    

The home is in Thousand Oaks, California. Do not include cold-climate or winterization assumptions such as winterizing pipes, snow preparation, or seasonal freeze protection.

First step: inspect the repository
----------------------------------

Before making changes:

1.  Inspect the complete repository structure.
    
2.  Identify:
    
    *   Unity version.
        
    *   Render pipeline.
        
    *   Existing scenes.
        
    *   Current WebGL template.
        
    *   Existing HTML, JavaScript, CSS, and Unity bridge code.
        
    *   Existing UI systems.
        
    *   Existing smart-home device abstractions.
        
    *   Existing room, object, asset, and interaction components.
        
    *   Existing authentication or backend integrations.
        
    *   Existing GitHub Pages deployment workflow.
        
3.  Determine the safest integration points.
    
4.  Create a concise implementation plan in a Markdown file before beginning major changes.
    
5.  Do not replace an existing scene, WebGL template, project configuration, or deployment workflow unless absolutely necessary.
    
6.  Prefer extending existing architecture over creating parallel systems.
    

Proceed with the implementation after inspection. Do not stop after producing the plan.

Recommended architecture
------------------------

Use a hybrid interface:

*   Unity handles:
    
    *   3D house navigation.
        
    *   Room and object selection.
        
    *   Spatial status indicators.
        
    *   Smart-home visualization.
        
    *   Asset highlighting.
        
    *   Maintenance-mode visualization.
        
*   HTML, CSS, and JavaScript handle:
    
    *   Google login.
        
    *   Task lists.
        
    *   Forms.
        
    *   Date pickers.
        
    *   Recurrence editors.
        
    *   Completion, snooze, and skip dialogs.
        
    *   Filters.
        
    *   History.
        
    *   Supply tracking.
        
    *   Responsive mobile layouts.
        
    *   Google Apps Script API communication.
        
    *   Offline cache and local fallback data.
        

The HTML maintenance interface should appear as an overlay or responsive side panel around the Unity canvas. It must work well on Android phones and desktop browsers.

Do not implement complex forms entirely as Unity UI unless the repository already has a strong and reusable Unity UI architecture that clearly makes this preferable.

Authentication and authorization
--------------------------------

Implement Google Sign-In for the web application.

Use configuration placeholders rather than hardcoded personal addresses:

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`   const AUTH_CONFIG = {    googleClientId: "REPLACE_WITH_GOOGLE_CLIENT_ID",    allowedEmails: [      "REPLACE_WITH_PRIMARY_EMAIL",      "REPLACE_WITH_SECONDARY_EMAIL"    ]  };   `

Requirements:

*   Only the two approved Google accounts may access household data.
    
*   Both users may:
    
    *   View all shared tasks.
        
    *   Create tasks.
        
    *   Edit tasks.
        
    *   Complete tasks.
        
    *   Snooze tasks.
        
    *   Skip tasks.
        
    *   View task history.
        
    *   Manage assets.
        
    *   Manage supplies.
        
    *   Assign tasks.
        
*   There are no private household tasks in version one.
    
*   The backend must validate identity and authorization.
    
*   Do not rely only on hiding UI elements in the browser.
    
*   Never place Google client secrets, API secrets, smart-home credentials, or private keys in the GitHub repository.
    
*   Include clear setup instructions for configuring OAuth and allowed users.
    

When Google authentication is not configured, support a clearly labeled local demo mode using mock household users.

Use configurable display names:

*   Brooks
    
*   Wife
    
*   Either
    
*   Shared
    

Do not require the real names to be committed to source control.

Data storage
------------

Use Google Sheets as the persistent database and Google Apps Script as the API.

Create a complete Apps Script backend and setup documentation.

Use the following sheet tabs.

### Users

Fields:

*   id
    
*   email
    
*   displayName
    
*   role
    
*   active
    
*   createdAt
    
*   updatedAt
    
*   version
    

### Rooms

Fields:

*   id
    
*   name
    
*   floor
    
*   zone
    
*   unityObjectId
    
*   notes
    
*   active
    
*   createdAt
    
*   updatedAt
    
*   version
    

### Assets

Fields:

*   id
    
*   name
    
*   type
    
*   category
    
*   roomId
    
*   locationId
    
*   unityObjectId
    
*   smartDeviceId
    
*   smartHomeProvider
    
*   manufacturer
    
*   model
    
*   serialNumber
    
*   purchaseDate
    
*   warrantyExpiration
    
*   manualUrl
    
*   photoUrl
    
*   notes
    
*   active
    
*   createdAt
    
*   updatedAt
    
*   version
    

### Tasks

Fields:

*   id
    
*   title
    
*   description
    
*   category
    
*   roomId
    
*   assetId
    
*   locationIds
    
*   unityObjectIds
    
*   assignedTo
    
*   assignmentMode
    
*   priority
    
*   status
    
*   dueDate
    
*   dueWindowStart
    
*   dueWindowEnd
    
*   recurrenceType
    
*   recurrenceConfig
    
*   nextDateStrategy
    
*   seasonalRegion
    
*   estimatedMinutes
    
*   defaultSnoozeDays
    
*   requiresReading
    
*   requiresMileage
    
*   requiresUsageCount
    
*   active
    
*   createdBy
    
*   createdAt
    
*   updatedAt
    
*   version
    
*   deletedAt
    

Store complex recurrence and multi-value fields as valid JSON strings where needed.

### TaskEvents

Use an append-only history table.

Fields:

*   id
    
*   taskId
    
*   eventType
    
*   eventDate
    
*   performedBy
    
*   previousDueDate
    
*   nextDueDate
    
*   notes
    
*   reason
    
*   cost
    
*   mileage
    
*   usageCount
    
*   timeSpentMinutes
    
*   readings
    
*   suppliesUsed
    
*   attachmentUrls
    
*   createdAt
    

Valid event types should include:

*   created
    
*   edited
    
*   completed
    
*   skipped
    
*   snoozed
    
*   reopened
    
*   assigned
    
*   archived
    
*   restored
    

### Supplies

Fields:

*   id
    
*   name
    
*   category
    
*   assetId
    
*   unit
    
*   quantity
    
*   reorderThreshold
    
*   reorderQuantity
    
*   preferredProductUrl
    
*   notes
    
*   active
    
*   createdAt
    
*   updatedAt
    
*   version
    

### TaskSupplies

Fields:

*   id
    
*   taskId
    
*   supplyId
    
*   defaultQuantityUsed
    
*   required
    
*   createdAt
    
*   updatedAt
    

### DeviceMappings

Fields:

*   id
    
*   assetId
    
*   unityObjectId
    
*   provider
    
*   deviceId
    
*   deviceType
    
*   capabilities
    
*   enabled
    
*   createdAt
    
*   updatedAt
    
*   version
    

### Settings

Fields:

*   key
    
*   value
    
*   description
    
*   updatedAt
    
*   updatedBy
    

Backend requirements
--------------------

The Apps Script API must:

*   Validate the signed-in Google user.
    
*   Reject unauthorized users.
    
*   Provide CRUD endpoints for all primary entities.
    
*   Append task events rather than overwriting history.
    
*   Support batch reads for efficient initial loading.
    
*   Support optimistic locking with the version field.
    
*   Return a clear conflict response when two users edit the same record.
    
*   Use soft deletion for tasks, assets, rooms, supplies, and mappings.
    
*   Sanitize all incoming values.
    
*   Validate JSON fields.
    
*   Return consistent JSON response structures.
    
*   Include useful error codes and human-readable messages.
    
*   Avoid exposing raw spreadsheet internals.
    
*   Use ISO 8601 date strings.
    
*   Account for the America/Los\_Angeles timezone.
    
*   Include a setup function that creates missing sheets and headers.
    
*   Include optional seed-data creation.
    
*   Include simple migration support if columns are added later.
    

Use a consistent response shape such as:

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`   {    "success": true,    "data": {},    "error": null,    "serverTime": "2026-07-12T12:00:00-07:00"  }   `

Offline and local behavior
--------------------------

The app must run before Google Sheets is configured.

Implement:

*   A local mock-data adapter.
    
*   A Google Apps Script data adapter.
    
*   A shared repository/service interface so the UI does not care which adapter is active.
    
*   LocalStorage or IndexedDB caching.
    
*   Offline read access.
    
*   Queued offline actions where practical.
    
*   A visible sync state:
    
    *   Synced
        
    *   Syncing
        
    *   Offline
        
    *   Conflict
        
    *   Error
        
*   Safe retry logic.
    
*   No silent data loss.
    

Prefer IndexedDB for structured cached data and an offline action queue. LocalStorage may be used for small settings only.

Task actions
------------

Every task must support:

*   Complete
    
*   Snooze
    
*   Skip
    
*   Edit
    
*   Reassign
    
*   View history
    
*   Archive
    

### Complete flow

When the user selects Complete:

1.  Show the calculated next due date.
    
2.  Offer:
    
    *   Use calculated date.
        
    *   Select another date.
        
    *   Complete with no next date.
        
3.  Allow optional entry of:
    
    *   Notes.
        
    *   Cost.
        
    *   Mileage.
        
    *   Usage count.
        
    *   Time spent.
        
    *   Measurements or readings.
        
    *   Supplies used.
        
    *   Attachment or Google Drive URLs.
        
4.  Record who completed it.
    
5.  Write a completed TaskEvent.
    
6.  Update supply quantities when supplies are used.
    
7.  Update task assignment when rotation applies.
    
8.  Update the next due date.
    
9.  Refresh Unity object status indicators.
    

### Snooze flow

Provide defaults:

*   Tomorrow
    
*   3 days
    
*   1 week
    
*   2 weeks
    
*   Custom date
    

Allow each task to define its preferred default snooze interval.

A snooze must:

*   Preserve the original due date in history.
    
*   Record the new due date.
    
*   Record the user.
    
*   Optionally record a reason.
    
*   Create a snoozed TaskEvent.
    

### Skip flow

A skip must:

*   Advance to the next valid recurrence.
    
*   Not count as a completion.
    
*   Preserve the skipped occurrence in history.
    
*   Allow an optional reason.
    
*   Create a skipped TaskEvent.
    
*   Preserve assignment rotation unless the task configuration explicitly says skipping advances the rotation.
    

Recurrence engine
-----------------

Support all common recurrence types.

Implement the recurrence logic as a well-tested, independent module.

Supported types must include:

### Fixed interval

*   Every X days.
    
*   Every X weeks.
    
*   Every X months.
    
*   Every X years.
    

### Weekly patterns

*   Specific weekdays.
    
*   Multiple weekdays.
    
*   Every X weeks on selected weekdays.
    

Examples:

*   Every Monday.
    
*   Monday and Thursday.
    
*   Every other Saturday.
    

### Monthly patterns

*   Specific day of month.
    
*   Last day of month.
    
*   First, second, third, fourth, or last weekday of month.
    
*   Every X months.
    

Examples:

*   First Saturday of every month.
    
*   Last Sunday every three months.
    

### Annual and seasonal patterns

*   Specific annual date.
    
*   Multiple annual dates.
    
*   Month-based seasonal schedules.
    
*   Region-aware seasonal labels without hardcoded winterization.
    

Examples appropriate for Thousand Oaks:

*   Spring yard cleanup.
    
*   Pre-summer HVAC inspection.
    
*   Late-summer irrigation inspection.
    
*   Fall gutter cleaning.
    
*   Wildfire-season defensible-space review.
    

### Completion-relative schedules

Next occurrence based on the actual completion date.

Example:

*   Replace litter 14 days after it was actually replaced.
    

### Schedule-relative recurrence

Next occurrence based on the original expected schedule.

Example:

*   Smoke detector test remains on a fixed six-month cadence even if completed late.
    

Each task must have:

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`   nextDateStrategy:  - completion_date  - scheduled_date  - user_choice   `

For user\_choice, prompt during completion and remember the task default.

### Due windows

Support:

*   Earliest acceptable completion date.
    
*   Target due date.
    
*   Overdue date.
    

Example:

*   Water plants around every 7 days.
    
*   Due window begins after 5 days.
    
*   Target is 7 days.
    
*   Overdue after 9 days.
    

### Mileage-based recurrence

Support:

*   Every X miles.
    
*   Calendar interval.
    
*   Whichever occurs first.
    

Example:

*   Oil change every 7,500 miles or 12 months, whichever comes first.
    

Allow manual mileage entry and retain mileage history.

Do not attempt to connect directly to Ford or vehicle APIs in version one. Create integration hooks only.

### Usage-based recurrence

Support:

*   Every X uses.
    
*   Every X cycles.
    
*   Every X operating hours.
    
*   Calendar fallback.
    
*   Whichever occurs first.
    

Examples:

*   Clean filter every 20 uses or 30 days.
    
*   Service an appliance after a configurable cycle count.
    

### Threshold and condition-based tasks

Create extensible support for future triggers such as:

*   Sensor reading exceeds a threshold.
    
*   Supply quantity falls below a threshold.
    
*   Smart-home device reports a fault.
    
*   Weather condition occurs.
    
*   Another task is completed.
    
*   A manual counter reaches a value.
    

For version one, implement the data model and evaluation interface. Fully support supply thresholds and manual counters. Mock external sensor and weather conditions.

### One-time tasks

Support tasks with no recurrence.

### On-demand tasks

Support tasks with no due date that are completed only when needed.

Assignment
----------

Tasks may be assigned to:

*   Brooks
    
*   Wife
    
*   Either
    
*   Shared
    

Support assignment modes:

*   Fixed person.
    
*   Either person.
    
*   Shared responsibility.
    
*   Strict alternating rotation.
    
*   Assign to whoever did not complete it last.
    
*   Manual rotation order.
    
*   Lowest active assigned workload.
    

Use “whoever did not complete it last” as the default rotating-task behavior.

Track all assignment changes in TaskEvents.

Both users must still be able to complete any task unless a future setting explicitly restricts it.

Categories
----------

Seed these categories:

*   Pets
    
*   Vehicle
    
*   House
    
*   Yard
    
*   Plants
    
*   Hot Tub
    
*   Cleaning
    
*   Appliances
    
*   Safety
    
*   Supplies
    
*   Smart Home
    
*   Seasonal
    
*   Other
    

Do not create a Baby category in the initial seed data. Provide an integration interface for the separate baby app instead.

Unity integration
-----------------

Add a reusable component similar to:

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`   using UnityEngine;  public class HomeAsset : MonoBehaviour  {      public string assetId;      public string displayName;      public string roomId;      public string category;      public string unityObjectId;      public string smartDeviceId;  }   `

Adapt this to existing project conventions if equivalent components already exist.

Requirements:

*   Unity objects may map to one asset.
    
*   One task may map to multiple Unity objects or locations.
    
*   Tasks without a dedicated object may map to a room, zone, driveway, yard, or generic household location.
    
*   Object mappings must use stable IDs rather than scene hierarchy paths alone.
    
*   IDs must survive object renaming where practical.
    
*   Add editor validation for missing or duplicate IDs.
    
*   Add a small editor utility for listing mapped assets and duplicate IDs if appropriate.
    

JavaScript and Unity messaging
------------------------------

Create a documented bidirectional bridge.

JavaScript should be able to call Unity for actions such as:

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`   unityMaintenanceBridge.highlightAsset(assetId);  unityMaintenanceBridge.highlightRoom(roomId);  unityMaintenanceBridge.enterMaintenanceMode();  unityMaintenanceBridge.exitMaintenanceMode();  unityMaintenanceBridge.refreshStatuses(statusPayload);   `

Unity should notify JavaScript of events such as:

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`   window.onUnityAssetSelected({    assetId: "hot-tub-main",    roomId: "backyard",    unityObjectId: "Backyard_HotTub"  });   `

Use the existing Unity WebGL bridge if present.

The bridge must fail safely when Unity is not loaded or the HTML UI is running in local mock mode.

Maintenance mode
----------------

Add a dedicated maintenance mode to the 3D house.

When active:

*   De-emphasize normal smart-home control visuals.
    
*   Highlight assets and rooms with open tasks.
    
*   Allow filtering by:
    
    *   Overdue.
        
    *   Due today.
        
    *   Due soon.
        
    *   Category.
        
    *   Assigned person.
        
    *   Priority.
        
*   Clicking a highlighted object opens its maintenance panel.
    
*   Objects with no tasks remain selectable but visually subdued.
    

Recommended visual states:

*   Overdue: strong urgent indicator.
    
*   Due today: clear warning indicator.
    
*   Due soon: subtle warning indicator.
    
*   Recently completed: brief success indicator.
    
*   No open task: neutral state.
    

Use the existing visual design system where available. Do not hardcode inaccessible color-only distinctions. Include icons, badges, outlines, or labels so status is understandable without relying solely on color.

Asset panel
-----------

Selecting a room or asset should open a responsive side panel with tabs or sections for:

*   Smart controls.
    
*   Open maintenance tasks.
    
*   Maintenance history.
    
*   Supplies.
    
*   Manuals and links.
    
*   Asset details.
    
*   Notes.
    
*   Photos or attachment links.
    

If real smart-home controls are not yet implemented for that asset, show a clean placeholder rather than a broken interface.

Smart-home architecture
-----------------------

Do not directly couple the maintenance system to Google Home, Home Assistant, Matter, SmartThings, or a vendor API.

Create a generic provider interface, adapted to the project’s language and architecture.

Conceptually:

Plain textANTLR4BashCC#CSSCoffeeScriptCMakeDartDjangoDockerEJSErlangGitGoGraphQLGroovyHTMLJavaJavaScriptJSONJSXKotlinLaTeXLessLuaMakefileMarkdownMATLABMarkupObjective-CPerlPHPPowerShell.propertiesProtocol BuffersPythonRRubySass (Sass)Sass (Scss)SchemeSQLShellSwiftSVGTSXTypeScriptWebAssemblyYAMLXML`   public interface ISmartHomeProvider  {      Task GetDeviceState(string deviceId);      Task ExecuteCommand(string deviceId, string command, object value);  }   `

For WebGL compatibility, abstract async behavior appropriately.

Include:

*   A mock provider.
    
*   Provider registry.
    
*   Device capability model.
    
*   Graceful unavailable state.
    
*   No embedded credentials.
    
*   Clear extension documentation.
    

Dashboard
---------

Create a hybrid dashboard.

The default view should include:

1.  Overdue tasks.
    
2.  Due today.
    
3.  Due this week.
    
4.  Upcoming.
    
5.  Recently completed.
    
6.  Low supplies.
    
7.  Assignment summary.
    
8.  Shortcut to maintenance mode in the Unity house.
    

Provide filters for:

*   Person.
    
*   Category.
    
*   Room.
    
*   Asset.
    
*   Priority.
    
*   Status.
    
*   Date range.
    
*   Recurrence type.
    

Provide search across:

*   Task titles.
    
*   Descriptions.
    
*   Assets.
    
*   Rooms.
    
*   Notes.
    

The dashboard must be useful even when the Unity canvas is hidden or unavailable on a smaller device.

Mobile behavior
---------------

Prioritize Android phone use.

Requirements:

*   Responsive layout.
    
*   Large touch targets.
    
*   Bottom navigation or another compact mobile navigation pattern.
    
*   Task actions accessible in one or two taps.
    
*   No hover-only functionality.
    
*   Date and time controls that work well on mobile.
    
*   Forms should avoid unnecessary modal nesting.
    
*   Unity view may become a separate tab or screen on narrow devices.
    
*   Task lists must remain usable if Unity WebGL performance is limited on a phone.
    
*   Respect safe-area insets.
    
*   Support portrait and landscape orientations.
    

PWA
---

Where compatible with the existing deployment, add:

*   Web app manifest.
    
*   Installable home-screen experience.
    
*   Service worker.
    
*   App icons or placeholder icon instructions.
    
*   Cached app shell.
    
*   Offline task viewing.
    
*   Offline action queue.
    
*   Update notification when a new app version is available.
    

Do not aggressively cache large Unity builds without a deliberate cache versioning strategy. Document the strategy and storage impact.

Notifications
-------------

Implement these version-one reminders:

### In-app reminders

*   Overdue badges.
    
*   Due-today section.
    
*   Due-soon section.
    
*   Low-supply warnings.
    
*   Assignment indicators.
    

### Daily email summary

Use a time-driven Apps Script trigger.

Default schedule:

*   8:00 AM America/Los\_Angeles.
    

Send each authorized user a personalized summary containing:

*   Their assigned overdue tasks.
    
*   Their assigned tasks due today.
    
*   Shared tasks due today.
    
*   Tasks due within seven days.
    
*   Low supplies.
    
*   A link to the app.
    

Allow this to be disabled in Settings.

### Weekly household summary

Default:

*   Sunday at 8:00 AM America/Los\_Angeles.
    

Include:

*   Completed tasks from the prior week.
    
*   Skipped tasks.
    
*   Remaining overdue tasks.
    
*   Upcoming high-priority tasks.
    
*   Supply warnings.
    
*   Workload by person.
    

### Google Calendar

Create an optional integration layer and setup instructions.

Do not automatically create calendar events for every task. Allow users to explicitly enable calendar reminders per task.

### Browser notifications

Implement only as progressive enhancement. Do not rely on browser push as the only reminder mechanism.

Seed task data
--------------

Create editable recommended seed tasks suitable for Thousand Oaks, California.

Use conservative, clearly editable defaults.

### Pets

*   Scoop kitty litter: daily.
    
*   Fully replace kitty litter: every 14 days, based on completion date.
    
*   Deep-clean litter box: every 30 days, based on completion date.
    

### Vehicle

Create an asset placeholder for:

*   2022 Ford F-150.
    

Seed:

*   Check vehicle mileage: monthly.
    
*   Change oil: every 7,500 miles or 12 months, whichever comes first.
    
*   Check tire pressure: monthly.
    
*   Rotate tires: every 7,500 miles.
    
*   Wash vehicle: every 3 weeks, based on completion date.
    
*   Inspect wiper blades: every 6 months.
    
*   Renew registration: annual one-time recurring date, configurable by user.
    

Clearly state that manufacturer documentation and actual vehicle usage should override seed intervals.

### Plants

*   Water indoor plants: every 7 days with a due window of 5–9 days.
    
*   Inspect indoor plants for pests: monthly.
    
*   Fertilize indoor plants: every 3 months.
    
*   Water outdoor potted plants: every 3 days as an editable warm-weather default.
    
*   Inspect irrigation: monthly.
    

Do not make automated watering decisions from weather data in version one. Add a future weather-provider interface.

### Hot tub

*   Test water chemistry: twice weekly.
    
*   Add chemicals: weekly reminder, but mark as reading-dependent.
    
*   Clean hot-tub filter: every 30 days.
    
*   Rinse hot-tub filter: every 14 days.
    
*   Drain and refill: every 4 months.
    
*   Inspect cover: monthly.
    
*   Clean cover: every 2 months.
    

For hot-tub events, support structured readings:

*   Sanitizer type.
    
*   Chlorine or bromine.
    
*   pH.
    
*   Total alkalinity.
    
*   Calcium hardness.
    
*   Water temperature.
    
*   Chemicals added.
    
*   Quantity added.
    

### Yard and exterior

*   Weed front yard: every 14 days, completion-relative.
    
*   Weed backyard: every 14 days, completion-relative.
    
*   Clean deck: every 2 months.
    
*   Inspect deck: every 6 months.
    
*   Clean gutters: twice yearly.
    
*   Inspect defensible space: every 3 months, with increased relevance before and during wildfire season.
    
*   Clear dry vegetation: monthly during configurable wildfire-season months.
    
*   Inspect exterior for pests: every 3 months.
    
*   Inspect irrigation leaks: monthly.
    

Do not include winterization tasks.

### House and appliances

*   Replace HVAC air filter: every 90 days.
    
*   Inspect HVAC filter: every 30 days.
    
*   Replace refrigerator water filter: every 6 months.
    
*   Test smoke detectors: every 6 months.
    
*   Test carbon monoxide detectors: every 6 months.
    
*   Replace smoke detector batteries: annual unless device-specific guidance says otherwise.
    
*   Clean dryer lint path: every 3 months.
    
*   Deep-clean dryer vent: annual.
    
*   Inspect washing-machine hoses: every 6 months.
    
*   Clean dishwasher filter: monthly.
    
*   Clean range hood filter: every 3 months.
    
*   Flush water heater: annual, clearly labeled as dependent on manufacturer guidance.
    
*   Inspect under sinks for leaks: every 3 months.
    
*   Review home emergency supplies: every 6 months.
    

Supply tracking
---------------

Seed optional supplies:

*   Kitty litter.
    
*   HVAC filters.
    
*   Refrigerator water filters.
    
*   Hot-tub sanitizer.
    
*   pH increaser.
    
*   pH decreaser.
    
*   Alkalinity increaser.
    
*   Hot-tub filter cleaner.
    
*   Plant fertilizer.
    
*   Weed-removal supplies.
    
*   Vehicle cleaning supplies.
    

Requirements:

*   Completing a task may reduce associated supply quantities.
    
*   The user may override the quantity used.
    
*   Low stock should appear on the dashboard.
    
*   Low stock may generate a new restock task.
    
*   Restock tasks must not duplicate repeatedly while one is already active.
    
*   Include a preferred product URL field, but do not integrate shopping APIs.
    

Conflict handling
-----------------

Because two people may edit simultaneously:

*   Use optimistic locking.
    
*   Display a clear conflict dialog.
    
*   Show the local value and latest server value.
    
*   Allow:
    
    *   Reload server version.
        
    *   Keep local changes as a new update.
        
    *   Merge fields manually.
        
*   Never silently overwrite a newer version.
    

History and auditability
------------------------

TaskEvents must be immutable after creation except for administrative correction support that creates an additional correction event.

Provide a readable history timeline showing:

*   Date.
    
*   Action.
    
*   User.
    
*   Previous due date.
    
*   New due date.
    
*   Notes.
    
*   Measurements.
    
*   Supplies used.
    
*   Cost.
    
*   Mileage.
    

Provide summary metrics without encouraging meaningless streak behavior.

Useful metrics:

*   Average completion delay.
    
*   Typical completion interval.
    
*   Most frequently snoozed tasks.
    
*   Most frequently skipped tasks.
    
*   Workload by category.
    
*   Workload by person.
    
*   Estimated time spent.
    
*   Recurring supply consumption.
    

Do not make streaks the central design concept.

Separate baby-app integration
-----------------------------

Do not recreate baby tracking.

Create a small future integration interface capable of:

*   Reading summary cards from another app.
    
*   Opening the baby app.
    
*   Displaying a limited notification count.
    
*   Receiving explicitly shared household reminders.
    

Use mock data only.

Do not store detailed baby health, feeding, sleep, medical, or other sensitive baby data in this maintenance application.

Visual design
-------------

Use a clean, calm household-control aesthetic.

Recommended direction:

*   Modern smart-home dashboard.
    
*   Warm but not childish.
    
*   Clear status hierarchy.
    
*   High contrast.
    
*   Accessible typography.
    
*   Minimal visual clutter.
    
*   Consistent with the existing Unity smart-home interface.
    
*   Dark and light modes if the existing app supports themes.
    
*   Avoid an overly corporate task-management appearance.
    
*   Avoid gamification.
    

Use existing fonts, tokens, and components where possible.

Accessibility
-------------

Implement:

*   Keyboard navigation.
    
*   Visible focus states.
    
*   Semantic HTML.
    
*   ARIA labels where needed.
    
*   Screen-reader-friendly task states.
    
*   Reduced-motion support.
    
*   Status indicators that do not depend only on color.
    
*   Accessible dialogs.
    
*   Sufficient contrast.
    
*   Large mobile touch targets.
    

Testing
-------

Add practical tests for:

*   Recurrence calculations.
    
*   Month-end recurrence.
    
*   Leap years.
    
*   Daylight-saving transitions.
    
*   Scheduled-date versus completion-date behavior.
    
*   Due windows.
    
*   Mileage-or-date triggers.
    
*   Usage-or-date triggers.
    
*   Snooze behavior.
    
*   Skip behavior.
    
*   Assignment rotation.
    
*   Supply deductions.
    
*   Authorization rejection.
    
*   Optimistic-lock conflicts.
    
*   Offline queue behavior.
    
*   JSON serialization.
    
*   Unity-JavaScript bridge payload validation.
    

Use the repository’s existing test tools where possible.

If no suitable JavaScript testing framework exists, add a lightweight test setup without introducing an unnecessary large build system.

For Unity code, add Edit Mode tests for pure logic where practical.

Security
--------

Follow these requirements:

*   No secrets in source control.
    
*   No smart-home access tokens in Google Sheets.
    
*   No trust based solely on frontend state.
    
*   Validate authorized Google identity server-side.
    
*   Sanitize user-generated strings.
    
*   Escape HTML.
    
*   Validate URLs before rendering links.
    
*   Use Content Security Policy guidance compatible with GitHub Pages and Unity WebGL.
    
*   Avoid eval.
    
*   Avoid unsafe dynamic script injection.
    
*   Do not expose exact address information.
    
*   Keep detailed home-layout data behind authenticated access.
    
*   Document the limitations of hosting a private household application on GitHub Pages.
    

GitHub Pages deployment
-----------------------

Preserve the existing deployment workflow when possible.

Ensure:

*   Correct relative paths.
    
*   Support for repository subdirectory hosting.
    
*   Unity WebGL assets load from GitHub Pages.
    
*   Service-worker scope is correct.
    
*   OAuth redirect origins are documented.
    
*   Apps Script API endpoint is configurable.
    
*   Build artifacts are not unnecessarily duplicated.
    
*   Deployment instructions are complete.
    

Configuration
-------------

Create an obvious configuration file or configuration section for:

*   Google OAuth client ID.
    
*   Allowed email addresses.
    
*   Apps Script endpoint.
    
*   Application URL.
    
*   Timezone.
    
*   Daily reminder time.
    
*   Weekly reminder schedule.
    
*   Default user display names.
    
*   Mock mode.
    
*   Unity integration flags.
    
*   Feature flags.
    
*   Smart-home provider selection.
    
*   Baby-app URL.
    

Do not scatter these values across multiple files.

Documentation
-------------

Create:

### README.md

Include:

*   Project overview.
    
*   Architecture.
    
*   Local development.
    
*   Unity integration.
    
*   WebGL build instructions.
    
*   GitHub Pages deployment.
    
*   Mock mode.
    
*   Google authentication setup.
    
*   Google Sheets setup.
    
*   Apps Script deployment.
    
*   Allowed-user setup.
    
*   Reminder trigger setup.
    
*   PWA behavior.
    
*   Troubleshooting.
    
*   Security limitations.
    
*   Future smart-home provider integration.
    
*   Future baby-app integration.
    

### docs/DATA\_MODEL.md

Document every sheet, field, data type, allowed value, and relationship.

### docs/RECURRENCE.md

Document recurrence configuration examples and calculation rules.

### docs/UNITY\_BRIDGE.md

Document JavaScript-to-Unity and Unity-to-JavaScript messages.

### docs/SETUP\_GOOGLE.md

Provide step-by-step Google Cloud, OAuth, Sheet, and Apps Script setup instructions.

### docs/IMPLEMENTATION\_PLAN.md

Record the initial repository assessment, design decisions, and integration plan.

Deliverables
------------

Complete all practical deliverables in the repository:

*   Integrated maintenance UI.
    
*   Unity maintenance mode.
    
*   Asset and room mapping.
    
*   Unity-JavaScript bridge.
    
*   Recurrence engine.
    
*   Complete, snooze, and skip flows.
    
*   Assignment and rotation.
    
*   Supplies.
    
*   History.
    
*   Google login integration.
    
*   Apps Script backend.
    
*   Google Sheets schema setup.
    
*   Mock local adapter.
    
*   Offline cache.
    
*   Responsive Android layout.
    
*   PWA configuration.
    
*   Daily and weekly email summaries.
    
*   Optional calendar-reminder foundation.
    
*   Seed data.
    
*   Tests.
    
*   Documentation.
    

Implementation constraints
--------------------------

*   Preserve existing smart-home behavior.
    
*   Do not replace functioning code merely to impose a preferred style.
    
*   Reuse existing architecture and naming conventions.
    
*   Avoid unnecessary frameworks.
    
*   Avoid adding npm dependencies unless they provide clear value.
    
*   Keep the frontend compatible with GitHub Pages.
    
*   Keep the backend entirely within Google Apps Script and Google Sheets for version one.
    
*   Avoid direct image uploads in version one.
    
*   Support attachment URLs and Google Drive links.
    
*   Do not commit generated credentials.
    
*   Do not implement real home-device commands unless the project already has a secure provider.
    
*   Use mock smart-home controls where real integrations are unavailable.
    
*   Make all seed schedules editable.
    
*   Clearly label health, safety, appliance, and vehicle schedules as configurable suggestions rather than authoritative service guidance.
    

Final validation
----------------

Before finishing:

1.  Run all available tests.
    
2.  Build the web frontend.
    
3.  Build or validate the Unity WebGL project where the environment permits.
    
4.  Verify relative GitHub Pages paths.
    
5.  Verify mock mode without Google configuration.
    
6.  Verify unauthorized-user handling.
    
7.  Verify mobile layout.
    
8.  Verify recurrence edge cases.
    
9.  Verify Unity bridge behavior.
    
10.  Verify offline startup.
    
11.  Verify no secrets were added.
    
12.  Review all modified files for unintended changes.
    

At the end, provide:

*   A summary of the implementation.
    
*   Major files created or changed.
    
*   Setup steps still requiring user credentials.
    
*   Any Unity Editor actions that could not be automated.
    
*   Tests run and their results.
    
*   Known limitations.
    
*   Recommended next steps for connecting real smart-home devices.
