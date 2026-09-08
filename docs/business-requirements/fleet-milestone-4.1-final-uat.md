# Fleet Milestone 4.1 Final UAT and Test Infrastructure

## Current sign-off status

Milestone 4.1 is **UAT-ready**. Audited full-pipeline run `uat-25690802143638-d7620e89` passed every build, database, routing, backend, frontend, Playwright, and cleanup gate. This status does not mean production-ready.

Machine-readable and human-readable latest-run reports are generated at:

- `tmp/fleet-m4.1-uat-results.json`
- `tmp/fleet-m4.1-uat-results.md`

## Architecture and isolation

`backend/Hop.FleetQaTool` is a database-direct QA command, not an API and not part of the production host. It refuses `Production` and refuses database names without `qa`, `test`, `uat`, or `ci`. It applies migrations, creates a unique `qa-{timestamp}-{random}` namespace, uses a fixed business clock of `2026-08-01T00:00:00Z`, writes a password-free JSON manifest, and deletes only manifest-owned rows in FK order.

The fixture creates isolated users/roles, department, vehicle/type, driver profile, maintenance type/schedules/record, document, request, assignment, trip, cancellation, and vehicle/driver unavailability. Passwords are generated in memory by the UAT runner. Cleanup also removes run-owned refresh tokens, audit records, notifications, deliveries, domain events, and outbox messages. Create/cleanup was validated on PostgreSQL 16.

No production schema change or new migration was required. No QA endpoint exists. No production roles receive Fleet permissions. Fleet production navigation remains unchanged.

## Commands

Developer Playwright mode:

```powershell
npm --prefix frontend run test:e2e:dev
```

Fail-fast UAT mode with disposable PostgreSQL 16, migrations, tests, local services, fixtures, cleanup, and reports:

```powershell
./scripts/run-fleet-m4.1-uat.ps1
```

Use `-KeepArtifacts` to retain the per-run directory. Credentials/tokens are not included in the manifest or summary.

## Test infrastructure

- Frontend: Vitest 1.6, jsdom, Testing Library, React Query/router/theme wrapper, direct API-layer mocks.
- Coverage scope: Fleet dashboard, calendar, maintenance form/detail, and vehicle documents. Latest coverage: statements 85.43%, branches 62.17%, functions 47.05%, lines 85.43%.
- PostgreSQL integration: real PostgreSQL 16, real EF migrations, `tstzmultirange` feature check, deterministic overlap and calendar projection datasets.
- Utilization expectation for the overlap-heavy vehicle: assigned 6h, trip 4h, unavailable 4h, maintenance 4h, blocked union 6h, available 18h, utilization 22.2222%.
- Playwright UAT: 22 named scenarios, one worker, no retry, fail-fast credentials/manifest, screenshots/traces/videos retained on failure.

## Latest validated results

| Gate | Result |
|---|---|
| Backend build | Passed |
| EF migration apply on disposable PostgreSQL 16 | Passed |
| Backend unit tests | 262 passed, 0 failed, 0 skipped |
| Fleet PostgreSQL integration | 2 passed, 0 failed, 0 skipped |
| Frontend lint | Passed |
| Frontend component tests | 13 passed, 0 failed |
| Frontend component coverage | Passed |
| Frontend build | Passed |
| QA fixture create | Passed |
| Local backend/frontend health | Passed |
| Authorized routing smoke through Vite | Passed |
| QA cleanup | Passed |
| Playwright UAT | 22 passed, 0 failed, 0 skipped, 0 flaky |
| Git diff check | Passed |
| Process cleanup | Passed |
| Docker container cleanup | Passed |

## Routing correction and verification

The failed routing approach proxied every `/fleet/*` request, which also captured SPA navigations such as `/fleet/dashboard` and sent them to backend API actions without an Authorization header. Fleet API-client requests are now normalized to `/api/fleet/*`; Vite proxies only `/api/*`, while `/fleet/*` browser navigation remains with the SPA. Authentication remains `/api/auth/*`, so it is never rewritten to `/api/api/auth/*`.

The runner performs authorized HTTP smoke checks through the Vite origin for login, dashboard summary, calendar, and maintenance. Each must return a parseable `application/json` response. The same full run applied and listed migrations through `20260802051021_AddFleetMaintenanceStartMileageConstraint` and generated an idempotent migration script in its isolated run artifacts.

## Final verdict

**Milestone 4.1 UAT-ready** based on run `uat-25690802143638-d7620e89`. Production rollout remains separately gated by configuration and secrets review, backup/rollback rehearsal, production migration dry run, role mapping approval, controlled rollout approval, monitoring/alerting validation, security review, and change approval.

## Scope protection and remaining risks

- Leave Module code was not changed.
- Compatibility Matrix, Emergency Transport Workflow, and Mobile Driver UX were not started.
- No production deployment or production migration occurred.
- No real users, drivers, vehicles, departments, or static business IDs are used.
- Component tests still emit non-failing React Router future warnings and a jsdom navigation warning; these should be cleaned up separately.
- npm reported dependency audit findings during installation; no forced dependency upgrade was applied because that would be outside this milestone's compatibility scope.
