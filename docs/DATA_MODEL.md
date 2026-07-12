# Data model

All dates are ISO 8601 strings. Date-only values use `YYYY-MM-DD`; event and
audit timestamps include an offset. IDs are opaque stable strings. Booleans are
stored as Sheets booleans, numbers as numbers, and the fields marked JSON as
valid JSON text. `version` begins at 1 and increments for every mutation.

## Users

| Field | Type | Meaning |
| --- | --- | --- |
| id | string | Stable user key (`primary`, `secondary`, or UUID) |
| email | string | Google account; must also be in the backend allowlist |
| displayName | string | Configurable household label |
| role | `admin` or `member` | Reserved for administrative corrections |
| active | boolean | Whether reminders and assignment include the user |
| createdAt / updatedAt | datetime | Audit timestamps |
| version | integer | Optimistic-lock version |

## Rooms

`id` is referenced by Assets and Tasks. Fields: `id` string, `name` string,
`floor` string, `zone` string, `unityObjectId` optional stable Unity ID, `notes`
string, `active` boolean, `createdAt`/`updatedAt` datetime, `version` integer,
and `deletedAt` optional datetime. A deleted room is retained for history.

## Assets

| Field | Type | Meaning |
| --- | --- | --- |
| id, name | string | Stable key and display name |
| type, category | string | Provider-neutral asset classification |
| roomId, locationId | optional string | Spatial relationships |
| unityObjectId | optional string | Stable ID used by the Unity bridge |
| smartDeviceId | optional string | Provider-side device key, never a credential |
| smartHomeProvider | optional string | Provider registry key |
| manufacturer, model, serialNumber | optional string | Product metadata |
| purchaseDate, warrantyExpiration | optional date | Ownership metadata |
| manualUrl, photoUrl | optional HTTPS URL | External documents only |
| notes | string | Household notes |
| active, createdAt, updatedAt, version, deletedAt | audit fields | Soft-deletion and locking |

## Tasks

| Field | Type / allowed values |
| --- | --- |
| id, title, description, category | strings |
| roomId, assetId | optional foreign-key strings |
| locationIds, unityObjectIds | JSON arrays of strings |
| assignedTo | `primary`, `secondary`, `either`, `shared` |
| assignmentMode | `fixed`, `either`, `shared`, `alternate`, `not_last`, `manual_rotation`, `lowest_workload` |
| priority | `low`, `normal`, `high`, `urgent` |
| status | `open`, `completed`, `snoozed`, `skipped`, `archived` |
| dueDate, dueWindowStart, dueWindowEnd | optional dates |
| recurrenceType | `none`, `on_demand`, `interval`, `weekly`, `monthly`, `annual`, `seasonal`, `mileage`, `usage`, `threshold` |
| recurrenceConfig | JSON object documented in `RECURRENCE.md` |
| nextDateStrategy | `completion_date`, `scheduled_date`, `user_choice` |
| seasonalRegion | string; defaults to Thousand Oaks, California |
| estimatedMinutes, defaultSnoozeDays | non-negative numbers |
| requiresReading, requiresMileage, requiresUsageCount | booleans controlling completion fields |
| active | boolean |
| createdBy | user ID |
| createdAt, updatedAt, deletedAt | datetimes |
| version | optimistic-lock integer |

## TaskEvents

This table is append-only. Correction creates a new `corrected` event rather
than modifying an existing row.

| Field | Type / meaning |
| --- | --- |
| id, taskId | event ID and task foreign key |
| eventType | `created`, `edited`, `completed`, `skipped`, `snoozed`, `reopened`, `assigned`, `archived`, `restored`, `corrected` |
| eventDate, createdAt | datetimes |
| performedBy | user ID or authorized email |
| previousDueDate, nextDueDate | optional dates |
| notes, reason | optional strings |
| cost, mileage, usageCount, timeSpentMinutes | optional non-negative numbers |
| readings | JSON object, for example hot-tub chemistry measurements |
| suppliesUsed | JSON array of `{supplyId, quantity}` |
| attachmentUrls | JSON array of HTTPS/Drive URLs |

## Supplies

Fields: `id`, `name`, and `category` strings; optional `assetId`; `unit` string;
numeric `quantity`, `reorderThreshold`, and `reorderQuantity`; optional HTTPS
`preferredProductUrl`; `notes`; plus `active`, timestamps, `version`, and
`deletedAt`. A quantity at or below its threshold is low. A restock automation
must check for an existing open restock task before creating another.

## TaskSupplies

The many-to-many task/supply join contains `id`, `taskId`, `supplyId`, numeric
`defaultQuantityUsed`, boolean `required`, and `createdAt`/`updatedAt`. Actual
usage is copied into the immutable completion event.

## DeviceMappings

Fields: `id`, `assetId`, `unityObjectId`, provider-neutral `provider`, `deviceId`,
`deviceType`, JSON `capabilities`, boolean `enabled`, timestamps, `version`,
`active`, and `deletedAt`. Credentials are explicitly not part of this table.

## Settings

Key/value records contain `key`, `value`, `description`, `updatedAt`, and
`updatedBy`. Initial keys include `schemaVersion`, `appUrl`,
`dailyEmailEnabled`, and `weeklyEmailEnabled`.

## Relationships

- Room 1 → many Assets and Tasks.
- Asset 1 → many Tasks, Supplies, and DeviceMappings.
- Task 1 → many immutable TaskEvents and TaskSupplies.
- Supply 1 → many TaskSupplies and usage entries embedded in TaskEvents.
- User IDs appear in task assignment and audit fields; deactivation never
  removes their historical identity.
