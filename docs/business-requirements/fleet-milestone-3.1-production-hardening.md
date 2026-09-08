# Fleet Milestone 3.1 — Production Hardening & UAT Readiness

## Architecture

Milestone 3.1 retains the Milestone 3 code-configured workflow engine and PostgreSQL durable outbox. Fleet-specific application services now own assignment replacement and notification recipient resolution. Leave controllers, workflow, notification queue and delegation routes are unchanged.

Assignment replacement runs in one EF transaction using `pg_advisory_xact_lock`, request and assignment optimistic-concurrency tokens, availability revalidation, audit, history, domain event and outbox insert. Existing assignment business fields are protected by a PostgreSQL trigger; replacement only closes the old row (`is_active/status/token`) and inserts a new row referencing it.

Recipient resolution is centralized in `FleetNotificationRecipientResolver`. It derives recipients from the request, department context, active/historical assignments, workflow return target, permission-bearing roles and valid `Scope=FLEET` delegations. A delegate must independently hold the required permission. Publisher-generated delivery rows are unique per event/user/channel; In-App dispatch additionally checks its delivery reference before insertion.

## Database migration

Migration `20260801165322_FleetProductionHardeningUatReadiness` is additive:

- `fleet_assignments.concurrency_token`, safely backfilled for existing rows
- `fleet_trip_records.is_aborted`, `aborted_at`, `aborted_by_user_id`, `abort_reason`
- abort-detail check constraint and user foreign key/index
- assignment immutable-fields trigger

The existing `replaced_assignment_id` column remains unchanged. API responses expose the business alias `ReplacementOfAssignmentId`, preserving previous clients and data. The Down migration removes only Milestone 3.1 additions.

## APIs

- `POST /api/fleet/assignments/{assignmentId}/replace`
- `POST /api/fleet/trips/{requestId}/mileage-override`
- `POST /api/fleet/trips/{requestId}/abort`
- Existing `/api/fleet/delegations` CRUD now returns user display names for the UAT UI

Replacement accepts optional vehicle and driver IDs; at least one must differ and reason is mandatory. Approved/execution states require `FleetDispatch.ReplaceApprovedAssignment`; earlier states require `FleetDispatch.ReplaceAssignment`.

## Permission and role matrix

| Capability | Permission | Intended UAT persona | Production assignment |
|---|---|---|---|
| Replace pre-approval assignment | `FleetDispatch.ReplaceAssignment` | Dispatcher | None |
| Replace approved assignment | `FleetDispatch.ReplaceApprovedAssignment` | Senior Dispatcher/Fleet Admin | None |
| Fleet delegation CRUD | `FleetDelegation.View`, `FleetDelegation.Manage` | Fleet Admin | None |
| Driver jobs and acknowledgement | `FleetDriver.ViewOwnJobs`, `FleetDriver.Acknowledge` | Driver | None |
| Start/complete trip | `FleetDriver.Start`, `FleetDriver.Complete` | Driver | None |
| Mileage override | `FleetTrip.OverrideMileage` | Fleet Admin | None |
| Abort trip | `FleetTrip.CloseByAdmin` | Fleet Admin | None |
| Dashboard/outbox diagnostics | `FleetDashboard.View`, `FleetOutbox.View` | Fleet Admin/UAT support | None |

The development seeder creates permission definitions only. It must not create `role_permissions` rows for Fleet codes. Fleet sidebar/navigation remains disabled.

## UAT scenarios

1. Requester creates/submits; Dispatcher assigns an available vehicle/driver.
2. Admin Reviewer approves; Director or valid Fleet delegate approves.
3. Driver sees only own job, accepts, starts with non-negative mileage and completes with `EndMileage >= StartMileage`.
4. Replace vehicle, driver and both; verify old assignment remains queryable and inactive, new row links to old, notifications go to requester/old/new driver and operational recipients.
5. Submit two replacement calls with the same tokens; exactly one succeeds and the other returns 409.
6. Verify unavailable vehicle, driver leave, expired licence and overlapping assignment are rejected.
7. Force LINE failure; In-App remains processed and retry does not create a duplicate In-App notification.
8. Request/approve/reject post-approval cancellation and verify active assignment behavior.
9. Override mileage and abort a trip as Fleet Admin; missing reason, stale token and ordinary driver are rejected.
10. Confirm Leave regression suite remains green, Fleet routes are not visible in production navigation, and no production role has Fleet permissions.

## Rollout gates and risks

- Run full build/test/lint/build/Playwright, EF idempotent script generation, migration rehearsal and `git diff --check`.
- Validate production-like recipient counts before enabling LINE to avoid broad permission-group broadcasts.
- Advisory locks require PostgreSQL; unit tests validate policy/model behavior while Docker/UAT validates real locking.
- LINE provider cannot guarantee exactly-once delivery after a network timeout; internal delivery is retry-safe/idempotent, but provider-side duplicate suppression remains an operational risk.
- No production deploy/migration, role assignment or navigation activation is included.

## Milestone 4 candidates

- Production rollout approval and controlled role mapping
- Full operations/KPI dashboard and notification observability
- Maintenance integration and compatibility matrix
- Emergency Ambulance workflow
- Load/concurrency testing and provider-level notification idempotency
- Optional adoption of shared workflow/outbox by other HOP modules after separate design review
