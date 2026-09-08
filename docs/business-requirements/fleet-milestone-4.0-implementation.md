# Fleet Milestone 4.0 — Controlled Rollout, Health and Observability

## Architecture

Milestone 4.0 preserves the shared Fleet workflow engine, scoped delegation and PostgreSQL outbox. A fail-closed rollout middleware now protects every `/api/fleet/*` operation except authenticated rollout inspection. Backend permissions remain mandatory after rollout access succeeds. Frontend routes and the Vehicle Booking menu apply the same rollout decision, but menu hiding is never treated as an authorization control.

Rollout state uses a singleton `fleet_rollout_settings` row as an auditable runtime override and falls back to the `Fleet` configuration section when no row exists. Default mode is `Disabled`. Updating the row requires `SystemSettings.Manage` or `FleetHealth.Manage`, a reason and concurrency token, and creates `Fleet.RolloutModeChanged` audit data.

Fleet Health reuses the existing EF context and configuration/options pattern. Thresholds are configuration-driven. Queries aggregate in PostgreSQL and avoid loading complete operational tables. Diagnostics never return credentials, tokens, full licence numbers or notification payloads.

## Feature flag guide

```json
"Fleet": {
  "RolloutMode": "Disabled",
  "UatUserIds": [],
  "UatRoleCodes": [],
  "WarningStuckHours": 24,
  "CriticalStuckHours": 72,
  "DeliveryRetryWarningCount": 3,
  "DeliveryFailedCriticalCount": 1
}
```

- `Disabled`: deny Fleet operational APIs and hide Fleet menu for everyone.
- `UATOnly`: allow users whose ID or role code is in the allowlist; permissions are still required.
- `Enabled`: permit permission-bearing users; this does not grant permissions.

Database override has precedence over configuration. No user, role or department ID is hardcoded. Do not use `Enabled` before the Stage 5 sign-off.

## Permission and role mapping

| Role | Minimum permissions | Optional permissions |
|---|---|---|
| Fleet Requester | `FleetRequest.ViewOwn/Create/EditOwn/Submit/Cancel` | `FleetRequest.Copy` |
| Fleet Dispatcher | `FleetDispatch.View/Assign/Return/Reject`, `FleetDispatch.ReplaceAssignment` | `FleetDispatch.ReplaceApprovedAssignment`, `FleetHealth.View` |
| Fleet Administration Reviewer | `FleetAdminReview.Approve/Return/Reject` | `FleetDelegation.View` |
| Fleet Director | `FleetDirector.Approve/Return/Reject` | `FleetDelegation.View/Manage` |
| Fleet Driver | `FleetDriver.ViewOwnJobs/Acknowledge/Start/Complete` | none |
| Fleet Admin | Fleet master/manage, replacement, cancellation, trip override, dashboard, health | `FleetOutbox.View`, `FleetDelegation.Manage` |
| Fleet Auditor | `FleetRequest.ViewAll`, `FleetReport.View`, `FleetHealth.View` | `FleetReport.Export`, `FleetOutbox.View` |
| SuperAdmin | `SystemSettings.View/Manage` for rollout inspection/change | explicit Fleet permissions only during approved UAT |

Migration and development seeding create definitions only. They never insert Fleet `role_permissions`. Permission diagnostics report missing/duplicate definitions and Fleet permissions found on production roles. Direct user-permission assignment is not supported by the current HOP authorization model; effective access is role-derived.

## APIs

- `GET /api/fleet/rollout/access`
- `GET /api/fleet/rollout/status`
- `PUT /api/fleet/rollout`
- `GET /api/fleet/health`
- `GET /api/fleet/health/issues?severity=`
- `GET /api/fleet/health/outbox`
- `GET /api/fleet/diagnostics/summary`
- `GET /api/fleet/diagnostics/outbox`
- `GET /api/fleet/diagnostics/stuck-workflows`
- `GET /api/fleet/diagnostics/permissions`

## Health rules

Overall state is `Critical` when at least one critical issue exists, otherwise `Warning` when warning issues exist, otherwise `Healthy`. A notification in `RETRY` is not an issue until its attempt count reaches `DeliveryRetryWarningCount`. In-App and LINE counts remain separate. Stuck workflow severity changes at configured warning/critical durations.

## Observability

Structured logs use named fields including correlation ID, request number, request/assignment IDs, actor/effective actor, delegation, workflow state, event type, outbox ID, channel, retry count and safe error status. Secrets and sensitive payloads are excluded.

## Migration and rollback

Migration `20260802041012_AddFleetControlledRolloutAndHealth` adds only `fleet_rollout_settings`, constraints/indexes and Fleet Health permission definitions. It assigns no roles. Rollback sequence:

1. Change rollout to `Disabled` and record reason.
2. Stop Fleet UAT activity and drain/inspect outbox.
3. Roll back application binaries.
4. Keep the additive table unless a reviewed database rollback is required; Down removes permission definitions only when unreferenced.

Leave schema, workflow, routes and permissions are unchanged.

## Deferred to ordered milestones

- 4.1: KPI queries/export, Maintenance and Fleet Calendar.
- 4.2: Compatibility Matrix, Emergency Transport workflow and expanded mobile driver UX.
