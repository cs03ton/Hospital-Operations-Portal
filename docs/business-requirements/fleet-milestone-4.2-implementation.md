# Fleet Phase 2.0 Milestone 4.2 Implementation

## Scope and safety boundary

This milestone adds a data-driven vehicle compatibility matrix, an internal Fleet emergency-transport path, and responsive web UX for drivers. Emergency transport is not EMS or an ambulance-dispatch system. It does not perform triage, clinical assessment, treatment recording, medical command, 1669 integration, medical-crew assignment, GPS tracking, live location, or automatic dispatch. The UI must display this boundary wherever an emergency request is created or reviewed.

## Compatibility matrix design and data dictionary

`fleet_capabilities` is the generic master. `DataType` is one of `BOOLEAN`, `NUMBER`, `TEXT`, or `ENUM`; typed values are stored in mutually exclusive columns and protected by database checks. `IsRequiredSafetyCapability` marks a requirement that can never be overridden. Generic definitions may be created by code/name, but no seed binds a capability to a real vehicle.

`fleet_vehicle_capabilities` stores effective-dated vehicle values. A partial unique index prevents more than one open-ended active value for a vehicle/capability pair. Old rows are retained for audit. `fleet_request_required_capabilities` stores the operator, typed required value, mandatory flag, and notes. Operators are validated against the capability type: Boolean uses `EQUALS`; Number uses `EQUALS`, `GREATER_THAN_OR_EQUAL`, or `LESS_THAN_OR_EQUAL`; Text uses `EQUALS`/`CONTAINS`; Enum uses `EQUALS`/`IN`.

Compatibility outcomes are `MATCH`, `PARTIAL_MATCH`, `NOT_MATCH`, and `OVERRIDDEN`. Mandatory mismatch blocks availability. Optional mismatch is partial and blocks normal dispatcher assignment. Override records are immutable, name every mismatch, require a reason and permission, and cannot include safety capabilities. Maintenance safety blocks, expired licences, inactive drivers, overlaps, leave, or vehicle/document blocks remain outside compatibility override policy.

## Emergency workflow guide and policy

Existing requests backfill to `Priority=NORMAL`. Priority ordering is `EMERGENCY`, `URGENT`, then `NORMAL`, with submitted time as the starvation-safe secondary sort. An emergency request requires reason, reporter, contact, incident location, requested departure, policy code, and post-review metadata.

The implemented path reuses existing states:

`DRAFT → PENDING_DISPATCH → PENDING_ADMIN_REVIEW → PENDING_DRIVER_ACK → READY → IN_PROGRESS → COMPLETED`

The normal admin/director path remains available. `bypass-approval` is limited to `EMERGENCY`, requires the explicit permission and reason, requires an active assignment, and re-runs availability/safety validation. It cannot bypass maintenance, capability safety requirements, driver status, licence expiry, leave, unavailability, or overlap. Assignment replacement, cancellation, decline, and trip abort continue to use the existing immutable/workflow paths.

Completed emergency trips retain their workflow history and require an immutable post-event review. Outcomes are `ACCEPTABLE`, `NEEDS_IMPROVEMENT`, or `POLICY_VIOLATION`. Review records do not rewrite prior transitions.

## Mobile driver guide

The responsive web routes are `/fleet/driver/jobs`, `/fleet/driver/jobs/:id`, and `/fleet/driver/trips/:id`. Cards expose operationally necessary data, priority text/badge, vehicle, passenger count, destination, and capability summary. Actions use mobile-sized targets, sticky safe-area spacing, double-submit protection, concurrency handling, and a reload-latest-state message when a network response is uncertain.

Map links encode the destination and open an external map. The application never reads the driver's current GPS position or stores location history. Trip start cannot reduce current vehicle mileage. Completion cannot be below start mileage. Start, completion, and attachment retry use idempotency keys. Attachments accept PDF/JPG/JPEG/PNG, run existing type and malware scanning, use safe generated names, and store under `fleet/trips/{yyyy}/{MM}/{requestId}`.

## Permissions and role matrix

New permissions cover capability view/manage, vehicle/request capability maintenance, compatibility view/override, emergency create/view/queue/dispatch/bypass/review/audit, driver job actions, trip start/complete/upload. Permission masters are seeded idempotently in development, but no new permission is assigned to a production role. Routes remain behind Fleet rollout and permission guards; production sidebar visibility is unchanged.

## Notification event catalogue and observability

Events include compatibility override and emergency submitted/assigned/bypassed/trip-completed/post-review-completed plus trip-attachment upload. The existing recipient resolver selects requester, reporter, active driver, dispatcher, reviewer, auditor, and valid Fleet delegates by permission. Outbox delivery remains per-channel and idempotent for In-App and LINE. Structured records use request/assignment/trip/actor/correlation/idempotency identifiers; full contact phone, licence number, tokens, passwords, and attachment contents must not be logged.

## Migration runbook and rollback

Migration `AddFleetCompatibilityEmergencyMobile` is additive: new tables, columns, indexes, foreign keys, and checks only. Apply to disposable/local PostgreSQL first, verify the latest migration, constraints, indexes, `priority=NORMAL` for legacy requests, and zero automatic production-role mappings. Generate an idempotent SQL script for review. Do not apply to production in this milestone.

Before rollout, rollback is to disable Fleet rollout/routes and revert the new migration only in an isolated environment. After real M4.2 data exists, prefer forward fixes; dropping the additive tables would destroy immutable audit history.

## Production rollout notes, UAT checklist, and risks

- Validate secrets, storage root, scanner, backups, restore rehearsal, monitoring, and alerting.
- Role mapping and Fleet navigation were approved after the passing Milestone 4.2 UAT run on 2026-08-04. Dispatcher and Driver permissions remain explicit Role Management assignments.
- Test 360×800 and tablet layouts, keyboard/numeric mileage input, focus/error states, horizontal overflow, conflict refresh, attachment retries, and external-map disclosure.
- Verify safety override rejection, concurrent dispatch, assignment replacement before accept, outbox retry, emergency post-review, calendar severity labels, and KPI counts.
- Remaining risks: policy configuration is intentionally narrow and still needs operational approval; external maps expose the request destination to the selected provider; no offline synchronization, GPS, auto-assignment, fairness score, native app, or EMS integration is included.
