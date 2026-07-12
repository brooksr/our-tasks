# Recurrence rules

The browser engine lives in `src/domain/recurrence.ts`. Calculations use date
keys anchored at UTC noon, which avoids accidental day changes around Los
Angeles daylight-saving transitions. The backend stores the result selected by
the user in the immutable event and current task row.

## Anchoring

- `completion_date`: calculate from the actual completion date. Use for litter
  replacement, weeding, and other work whose interval restarts when performed.
- `scheduled_date`: calculate from the prior due date even when completed late.
  Use for safety checks that should remain on a fixed cadence.
- `user_choice`: show the calculated value during completion and let the user
  retain it, select another date, or choose no next date.

Completion writes a `completed` event. Skip advances to the next recurrence but
writes `skipped`, not `completed`. Snooze only moves the current occurrence and
preserves both dates in a `snoozed` event.

## Configuration examples

```json
{ "recurrenceType": "interval", "recurrenceConfig": { "interval": 14, "unit": "days" } }
```

Units are `days`, `weeks`, `months`, or `years`. Month/year arithmetic clamps to
the last valid day: January 31 + one month becomes February 28 or 29.

```json
{ "recurrenceType": "weekly", "recurrenceConfig": { "interval": 1, "weekdays": [1, 4] } }
```

Weekdays use JavaScript numbering: Sunday 0 through Saturday 6. `interval: 2`
with `[6]` means every other Saturday.

```json
{ "recurrenceType": "monthly", "recurrenceConfig": { "interval": 3, "weekday": 0, "weekdayOrdinal": -1 } }
```

`dayOfMonth` can be 1–31 or `"last"`. `weekdayOrdinal` can be 1, 2, 3, 4, or
-1 for last. The example is the last Sunday every three months.

```json
{ "recurrenceType": "annual", "recurrenceConfig": { "annualDates": ["04-01", "10-01"] } }
```

Annual dates are `MM-DD`. Leap-day schedules clamp through interval-year logic;
multiple annual dates choose the first strictly after the anchor.

```json
{ "recurrenceType": "seasonal", "recurrenceConfig": { "months": [5, 6, 7, 8, 9, 10] }, "seasonalRegion": "Thousand Oaks, CA" }
```

Seasonal months are editable labels/schedules, not weather automation. Initial
recommendations focus on irrigation, dry vegetation, gutters, HVAC, and
wildfire defensible space—there are no freeze or snow assumptions.

```json
{ "recurrenceType": "mileage", "recurrenceConfig": { "mileageInterval": 7500, "calendarFallback": { "interval": 12, "unit": "months" } } }
```

Mileage and usage types become due when their counter threshold or calendar
fallback is reached, whichever comes first. Usage uses `usageInterval` and can
represent uses, cycles, or operating hours in the task description. All manual
readings remain in event history.

```json
{ "recurrenceType": "threshold", "recurrenceConfig": { "threshold": { "source": "supply", "operator": "lte", "value": 1 } } }
```

Supply and manual thresholds can be evaluated in v1. Sensor and weather sources
are modeled but intentionally mocked until a provider is registered.

`none` is a one-time task. `on_demand` has no due date and returns to open after
completion without calculating a next occurrence.

## Due windows

`dueWindowStart` is the earliest acceptable completion date, `dueDate` is the
target, and `dueWindowEnd` is the overdue boundary. The UI currently groups by
target date and retains the full window for editor/metrics use. A plant task can
therefore store 5, 7, and 9 days without pretending that day 6 is overdue.

## Assignment rotation

Rotation is independent from recurrence. `not_last` (the default) assigns the
next occurrence to the other household member based on immutable completion
history. A skipped occurrence does not advance rotation unless
`skipAdvancesRotation` is true. `manual_rotation` uses `rotationOrder`; lowest
workload compares active assignments at calculation time.
