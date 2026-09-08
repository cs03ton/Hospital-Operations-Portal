# HOP Phase 2.0 — Fleet Milestone 3

## Architecture and decisions

Fleet is the first consumer of an additive, code-configured shared workflow engine. Controllers submit a typed transition request; the engine validates state, action, permission, reason, return target and scoped delegation, then returns an immutable result for the Fleet application layer to persist with status history and audit in one EF Core unit of work. Leave remains on its existing workflow.

The notification boundary is a durable PostgreSQL outbox. Domain event, outbox message and per-channel delivery records are inserted with domain changes. `OutboxProcessor` dispatches In-App and LINE independently with idempotency keys, retry counts and terminal `FAILED` state. No controller sends LINE directly.

All instants are UTC. Clients render `Asia/Bangkok`. Existing `User` remains the personnel source. Fleet navigation and production-role permission assignment remain disabled.

## Workflow

```text
PENDING_DISPATCH -> PENDING_ADMIN_REVIEW
PENDING_ADMIN_REVIEW -> PENDING_DIRECTOR | RETURNED | REJECTED
PENDING_DIRECTOR -> PENDING_DRIVER_ACK | RETURNED | REJECTED
PENDING_DRIVER_ACK -> READY | RETURNED(DISPATCHER)
READY -> IN_PROGRESS -> COMPLETED
APPROVED/PENDING_DRIVER_ACK/READY -> CANCELLATION_PENDING -> CANCELLED|previous state
```

Director approval emits both the approval notification and driver acknowledgement request semantics; the persisted operational target is `PENDING_DRIVER_ACK`. The intermediate `APPROVED` constant remains available for imported/legacy transitions and cancellation rules.

## Shared delegation

`approval_delegations` is retained as the core table and extended with `scope`, `required_permission_code`, UTC `[start_at,end_at)`, and `concurrency_token`. Migration backfills legacy rows to `LEAVE` / `LeaveApproval.ApproveCurrentStep` using Bangkok day boundaries. Leave API filters to `LEAVE`; Fleet API filters to `FLEET`. A delegate must independently hold the required permission; delegation never grants permission. Fleet prevents overlap for the same delegator/scope/permission.

## Schema

- Extended: `approval_delegations`, `audit_logs`, `fleet_cancellation_requests`
- Added: `fleet_trip_records`, `domain_events`, `outbox_messages`, `notification_deliveries`
- Important indexes: outbox status/available time, unique event, unique delivery idempotency key, delegation resolution, unique trip request/assignment, existing partial active-assignment indexes.
- Migration: `20260801162044_AddFleetApprovalExecutionOutbox` (additive; downgrade drops only Milestone 3 objects/columns).

## API

- `GET /api/fleet/admin-review`; `POST /api/fleet/admin-review/{id}/{approve|return|reject}`
- `GET /api/fleet/director-approval`; `POST /api/fleet/director-approval/{id}/{approve|return|reject}`
- `GET|POST|PUT|DELETE /api/fleet/delegations`
- `GET /api/fleet/driver-jobs`; `GET /api/fleet/driver-jobs/{id}`; accept/decline actions
- `POST /api/fleet/trips/{id}/start|complete`
- `GET|POST /api/fleet/cancellations`; approve/reject review actions
- `GET /api/fleet/dashboard`
- `GET /api/fleet/outbox`

Queue/detail endpoints enforce permissions; driver endpoints additionally enforce ownership of the active assignment. Mutations require concurrency tokens.

## Permissions

Fleet Admin Review and Director action permissions follow `FleetAdminReview.*` and `FleetDirector.*`. New isolated capabilities are `FleetDelegation.View/Manage`, `FleetDriver.ViewOwnJobs`, `FleetDispatch.ReplaceAssignment`, `FleetDispatch.ReplaceApprovedAssignment`, `FleetDriver.StartTrip/CompleteTrip`, `FleetTrip.OverrideMileage/CloseByAdmin`, `FleetCancellation.Review`, `FleetDashboard.View`, and `FleetOutbox.View`. Seeds create permission definitions only; they do not assign production roles.

## Events and recipients

Catalogue includes RequestSubmitted, RequestReturned, RequestRejected, Assigned, AssignmentReplaced, AdminReviewApproved, DirectorApproved, DriverAccepted, DriverDeclined, CancellationRequested/Approved, TripStarted/Completed and TripClosedByAdmin. Thai templates are centralized. Recipient resolution must use permission-scoped operational groups plus requester/active driver; payloads contain request number/status only and never a full licence number.

## Frontend

Permission-guarded routes: `/fleet/review`, `/fleet/review/:id`, `/fleet/director`, `/fleet/director/:id`, `/fleet/delegations`, `/fleet/driver`, `/fleet/driver/:id`, `/fleet/dashboard`. Times use Bangkok locale rendering. Routes exist for QA, but sidebar navigation is intentionally unchanged.

## Migration and rollout

Milestone 2 was applied to local Docker first. The Milestone 3 idempotent script is generated at `tmp/fleet-m3-idempotent.sql`; Milestone 3 was then applied only to local Docker. Production requires backup, rehearsal, permission design, notification recipient configuration and QA sign-off. Rollback stops the worker first, then uses the reviewed EF Down migration only if no Milestone 3 data must be retained.

## Validation and known gaps

Solution build, backend regression tests, frontend lint/build and migration generation are required gates. The current delivery establishes the shared engine/outbox, approval, delegation, driver/trip, cancellation, dashboard and diagnostics foundations. Before production rollout, complete recipient resolution (current transition publishers may create events without deliveries), assignment replacement API/UI with advisory-lock integration, admin mileage override/abort UI, full CRUD Fleet delegation UI, richer pagination/KPIs, and controller-level integration/concurrency/outbox failure tests.

Deferred by scope: Emergency Ambulance, compatibility matrix, fairness score, external broker, visual/dynamic workflow designer, full Leave migration, email, full maintenance module, production role assignments, navigation rollout and production deployment.
