# Milestone 2 — Requester and Dispatcher Workflow

## Scope delivered

- Requester: create/edit draft, own list/detail, submit and cancel
- Dispatcher: queue, availability explanation, assign vehicle/driver, return to requester and reject
- Transaction tables: requests, passengers, status histories, immutable assignment history and cancellation records
- Request number `VH-yyyyMM-####` protected by PostgreSQL transaction advisory lock and unique index
- UTC persistence; frontend converts browser-local Bangkok input to ISO UTC and renders `DD/MM/YYYY HH:mm`
- optimistic concurrency through `concurrency_token`
- status history and audit rows are written in the same save/transaction as transitions

## Milestone 2 state transitions

```text
DRAFT -> PENDING_DISPATCH                 requester submit
RETURNED(REQUESTER) -> PENDING_DISPATCH   requester resubmit
PENDING_DISPATCH -> RETURNED(REQUESTER)   dispatcher return
PENDING_DISPATCH -> REJECTED              dispatcher reject
PENDING_DISPATCH -> PENDING_ADMIN_REVIEW  dispatcher assign
eligible pre-director states -> CANCELLED requester cancel
```

`RETURNED(DISPATCHER)` is represented in the schema/resolver contract for Milestone 3 reviewer returns. Milestone 2 does not expose an Admin Reviewer action that creates it.

## Availability

Vehicle checks: active/status, passenger capacity, requested type, vehicle unavailability and overlapping active assignments.

Driver checks: active user/profile/status, license validity through expected return, vehicle capability, driver unavailability, approved leave in Asia/Bangkok local dates and overlapping active assignments. Results contain availability plus Thai reasons; no auto-assignment, compatibility matrix or fairness score.

## Delegation boundary

Milestone 2 does not integrate delegation. UI/API remain module-specific. Milestone 3 will evolve the existing `ApprovalDelegation` table/service into a shared core using `Scope + RequiredPermissionCode + UTC StartAt/EndAt`; Fleet UI/API will only manage `Scope=FLEET`, while Leave UI/API will only manage `Scope=LEAVE`.

## Operational safety

- Fleet navigation remains disabled
- Fleet permissions are seeded but not assigned to production roles automatically
- migration is additive and does not change leave tables
- Emergency Ambulance, full compatibility matrix, fairness score, Admin Review, Director Approval and delegation integration are deferred
