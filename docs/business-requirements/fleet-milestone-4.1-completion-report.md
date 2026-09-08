# Fleet Milestone 4.1 Completion and UAT Readiness

## Architecture

`FleetUtilizationQueryService` executes one parameterized PostgreSQL 16 query. Every interval is clamped to `[startUtc,endUtc)`, empty ranges are discarded, and `range_agg(tstzrange)` produces non-overlapping multiranges before duration is summed. No identifiers or filters are concatenated into SQL.

`FleetWorkflowDurationQueryService` aggregates trusted lifecycle timestamps in PostgreSQL. `FleetMaintenanceMileageAuthorizationService` is the single conditional authorization boundary for maintenance mileage anomalies. Calendar projections remain read-only and availability remains the source of truth.

## Vehicle utilization formulas

- Reporting Window Hours: `endUtc - startUtc`.
- Assigned Hours: union of requested departure/return intervals for every historical or active assignment.
- Trip Hours: union of actual start to complete/abort; an ongoing operational query ends at the captured UTC snapshot.
- Unavailable Hours: union of vehicle-unavailability intervals.
- Maintenance Hours: union of blocking in-progress maintenance intervals.
- Blocked Hours: union of unavailability and blocking maintenance together; overlap is counted once.
- Available Hours: `max(Reporting Window Hours - Blocked Hours, 0)`.
- Utilization: `Trip Hours / Available Hours × 100`; it is `null` when available hours are zero.
- Distance: sum of non-negative `EndMileage - StartMileage` for trips in range.

Exports use a captured query time. An ongoing trip therefore has an explicit snapshot boundary and does not change inside one response.

## Workflow-duration contract

Statistics return average, sample count, minimum, maximum, and P90 minutes. Missing samples return `Average=null, SampleCount=0`. The latest successful status occurrence inside each request is used so a returned/re-submitted cycle does not mix an abandoned early cycle with the final successful cycle.

| Metric | Start | End | Inclusion |
| --- | --- | --- | --- |
| Draft to submit | request created | submitted | submitted requests |
| Dispatch | latest pending dispatch | latest pending admin review | ordered successful pair |
| Admin review | latest pending admin review | latest pending director | ordered successful pair |
| Director approval | latest pending director | latest approved/pending driver acknowledgement | ordered successful pair |
| Driver acknowledgement | director completion | latest ready | accepted cycle only |
| Waiting before trip | latest ready | latest in progress | started trips |
| Trip duration | latest in progress | completed/aborted | completed or aborted trips |
| Total lead time | request created | completed/aborted | terminal trips |
| Cancellation resolution | cancellation requested | reviewed/completed | resolved cancellations |
| Assignment replacement | replaced assignment created | replacement created | replacement records |

## Calendar catalogue

Stable IDs are prefixed by source: `request:`, `assignment:`, `trip:`, `vehicle-unavailability:`, `driver-unavailability:`, `maintenance:`, `document:`, and `cancellation:`. The API supports event-type, vehicle, driver, department, status, cancellation, and bounded-date filters. Event metadata excludes license and full document numbers. Document expiry is emitted as an all-day event and displayed in `Asia/Bangkok`.

## Maintenance frontend

Routes cover list/filter, create, edit with concurrency token, detail, start, complete, cancel, vehicle maintenance, and vehicle documents. Complete supports cost, mileage, vendor, invoice, result, override reason, and attachments. Documents support masked list, create, expiry badge, and disable. Attachment upload accepts PDF/JPG/JPEG/PNG; backend scanning/MIME/size validation remains authoritative, while download and soft-delete are permission guarded.

## Mileage override policy

Normal mileage at or above current vehicle mileage requires no override permission. Lower submitted mileage requires `FleetMaintenance.OverrideMileage` plus a non-empty reason. The operation runs under PostgreSQL advisory lock and optimistic tokens. The observed mileage is retained on the maintenance record, vehicle current mileage never decreases, and `FleetMaintenance.MileageOverride` audit/event data records previous, submitted, retained, actor/effective actor, delegation, reason, and correlation ID. Delegation never grants the underlying permission.

## Playwright UAT

Milestone 4.1 final UAT no longer accepts manually supplied maintenance or vehicle IDs. `Hop.FleetQaTool` creates a run-isolated manifest containing all business IDs. Developer mode may skip when a manifest or credential is absent; UAT mode fails preflight and never treats a skip as a pass. See `fleet-milestone-4.1-final-uat.md`.

## Production-readiness checklist

- Fleet rollout remains disabled by default and every new route uses rollout and permission guards.
- No Fleet permission is assigned to production roles.
- No Leave schema or behavior is changed.
- Migration is additive and is applied only to local Docker during development validation.
- Production deployment is prohibited until Playwright runs with UAT credentials and operational approval is recorded.
- Compatibility Matrix, Emergency Transport Workflow, and Mobile Driver UX remain entirely deferred to Milestone 4.2.
