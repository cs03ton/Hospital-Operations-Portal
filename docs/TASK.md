# TASK: Phase 1 Production Deploy Readiness

Goal: prepare User Management and Leave Management for Phase 1 production deployment.

Scope is locked to:

- Authentication/Login
- User Management
- Department Management
- Role/Permission Management
- Permission Enforcement
- Audit Log
- Leave Management
- Leave Approval Workflow
- Leave Balance
- Leave Attachment Upload/Download
- Leave PDF Export
- LINE Notification for leave workflow
- Dashboard for leave metrics only
- Basic Backup/Restore
- Production Environment Configuration

Do not add new modules outside User Management or Leave Management during Phase 1.

## Current Status Summary

Completed or mostly completed:

- JWT login, refresh token, logout, session revoke, login lockout, and optional cookie token mode with CSRF protection.
- User, department, role, permission, and audit log APIs with backend permission policies.
- Frontend protected routes and action-level `PermissionGuard` for core admin and leave pages.
- Leave request draft, submit, cancel, approve, reject, approval chain, delegation, escalation, balance, holiday, attachment, PDF, calendar, and report foundations.
- LINE Messaging delivery logging and retry worker foundation.
- Dashboard leave metrics from real leave tables.
- PostgreSQL EF Core migrations and migration smoke test support.
- Backend unit/integration tests and optional PostgreSQL E2E tests.
- CI backend build/test, migration smoke test, and frontend build.
- Basic backup/restore documentation.

Production hardening completed in this pass:

- Production schema path is migration-only; Docker Compose no longer mounts `database/schema.sql` and `database/seed.sql` into PostgreSQL init scripts.
- Startup seeding is controlled by `Database__SeedOnStartup`.
- The seeder no longer calls `EnsureCreatedAsync`; EF Core migrations must be applied first.
- Production seeding failures are no longer silently ignored.
- Default development admin creation is gated by `Seed__CreateDefaultAdmin`.
- Production admin bootstrap requires explicit username/password configuration and rejects the development password in production.
- Fresh DB smoke test commands now include explicit migration-first and seed/bootstrap configuration.
- Phase 1 frontend navigation and routes expose only Dashboard, User Management, Department Management, Role/Permission, Audit Log, Leave Management, and Leave reports.
- Dashboard UI displays only leave-related metrics.
- Phase 1 seed removes Repair, Borrow, and Inventory permission groups if they were created by earlier seeds.

Still not production-ready yet:

- Fresh Docker database smoke test has documented commands, but must be run and signed off on a Docker Desktop machine with daemon running.
- Production admin bootstrap must be rehearsed with the real hospital admin account and then disabled.
- Production `.env` and CORS origins must be finalized for the hospital deployment URL.
- Backup/restore commands are documented but not yet verified against a disposable restore database in this environment.

## Priority 1: Must Finish Before Deploy

1. Run and record Fresh DB Smoke Test
   - Start disposable PostgreSQL with Docker Desktop.
   - Run EF Core migrations against the disposable database.
   - Start backend with production-like env.
   - Confirm `/healthz`.
   - Confirm critical tables exist.
   - Confirm seed data exists.
   - Confirm default admin login works only in non-production or rotated admin works in production.
   - Record exact result in `docs/TESTING.md`.

2. Decide and enforce production database initialization strategy
   - Status: done for deployment configuration.
   - EF Core migrations are the production source of truth.
   - Docker Compose no longer mounts `database/schema.sql` and `database/seed.sql` as PostgreSQL init scripts.
   - Docker/deploy docs distinguish reference SQL from production migration.
   - Verify `database/schema.sql` and `database/seed.sql` remain aligned with latest EF model and `DevelopmentDataSeeder`.

3. Harden production seeding
   - Status: done for startup behavior and documentation.
   - Schema creation responsibility was removed from startup seeding.
   - Production seeding failures now fail startup instead of being hidden.
   - Development admin creation is gated by environment/config.
   - Production admin bootstrap is documented and requires explicit non-default credentials.

4. Lock Phase 1 menu and permission exposure
   - Status: done for frontend route/menu and Phase 1 permission seed.
   - Hide or disable frontend routes/menu entries for non-Phase-1 modules: repairs, borrowing, vehicles, meeting rooms, materials, inventory, and generic reports.
   - Keep API code for future modules untouched if already present, but do not expose them in Phase 1 UI.
   - Permission seed groups no longer include RepairManagement, BorrowManagement, or InventoryManagement.

5. Production environment configuration
   - Create final production `.env` checklist from `.env.example`.
   - Set strong `JWT_SECRET`.
   - Set real database user that is not PostgreSQL superuser.
   - Set exact CORS allowed origins.
   - Set `ASPNETCORE_ENVIRONMENT=Production`.
   - Set hospital name and branding values from configuration.
   - Set storage path to a mounted backup volume.
   - Set LINE token only via environment variable or secret manager.

6. Verify Authentication and Authorization
   - Test login, refresh, logout, session revoke, and lockout.
   - Test localStorage mode if still used for Phase 1.
   - Test cookie mode only with CSRF enabled if chosen for production.
   - Confirm every Phase 1 endpoint requires permission policies.
   - Confirm users cannot access other users' leave PDFs or attachments.

7. Verify Leave Management happy paths and denial paths
   - Create user, department, role, permissions.
   - Create leave type, balance, approval chain, and holiday.
   - Create draft leave request.
   - Validate working days and overlapping leave.
   - Upload/download allowed attachment.
   - Reject disallowed and oversize attachment.
   - Submit leave request.
   - Approve and reject leave request.
   - Verify approval history, balance pending/used updates, PDF export, LINE delivery log, and dashboard metrics.

8. Verify Audit Log for security-sensitive actions
   - Login success/failure/lockout.
   - Permission denied.
   - Leave create/update/submit/cancel/approve/reject.
   - Attachment upload/download/delete/scan failure.
   - PDF generation.
   - Audit export.
   - Refresh token reuse.

9. Verify backup and restore
   - Run `pg_dump` against a test database.
   - Restore into a new disposable database.
   - Start backend against restored database.
   - Confirm login, user list, leave list, attachment metadata, and dashboard metrics.
   - Verify uploaded file volume backup/restore procedure separately from database restore.

10. Final build/test gate
    - `dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj`
    - PostgreSQL E2E tests with `HOP_E2E_CONNECTION_STRING`
    - EF migration smoke test
    - `npm ci`
    - `npm run build`
    - Docker Compose config validation with production-like env.

## Priority 2: Should Finish Before Pilot

1. Pilot role matrix
   - Define roles for Phase 1: HR/Admin, Department Head, Staff, Director if needed.
   - Grant only required permissions per role.
   - Add a manual QA checklist for each role.

2. LINE pilot readiness
   - Confirm LINE channel token and secret are configured outside source code.
   - Map pilot users to `line_user_id`.
   - Test sent, failed, disabled, and retry states.
   - Confirm leave workflow does not fail if LINE fails.

3. Attachment operations readiness
   - Confirm storage volume permissions.
   - Confirm max size and allowed extensions.
   - Confirm ClamAV container behavior if file scanning is enabled.
   - Decide fail-closed or fail-open policy for pilot.

4. Monitoring readiness
   - Review `GET /api/monitoring/security-summary`.
   - Define manual alert checks for permission denied, failed upload, LINE failure, scan failure, login lockout, and refresh token reuse.
   - Add deployment runbook steps for checking logs after go-live.

5. Production docs cleanup
   - Update README with Phase 1 scope only.
   - Mark non-Phase-1 modules as future roadmap.
   - Add deployment sign-off checklist.
   - Add rollback checklist.

## Priority 3: Can Do After Deploy

1. Move access token fully away from localStorage after cookie mode is pilot-tested.
2. Add centralized monitoring/exporter such as OpenTelemetry, Prometheus, or ELK.
3. Add automated scheduled backup job and retention verification.
4. Add frontend code splitting to reduce large Vite bundle warning.
5. Add advanced audit retention UI and automated archival.
6. Improve report formatting and add more management reports.
7. Resume non-Phase-1 modules only after Phase 1 stabilizes.

## Known Bugs / Risks Before Deploy

| Area | Issue | Risk | Required Action |
| --- | --- | --- | --- |
| Database startup | Seeder now requires migrations first | Backend seed will fail if migrations were skipped | Run EF migration before startup seeding |
| Docker DB init | Compose now uses migration-only production path | Fresh DB requires explicit migration command | Run smoke test command sequence in `docs/TESTING.md` |
| Default admin | Development credentials remain documented for local use | Critical security risk if used in production | Bootstrap real admin with strong temporary password and disable `Seed__CreateDefaultAdmin` |
| Scope exposure | Future placeholder pages remain in source code but are not routed/menu-exposed | Low if routes stay disabled | Keep non-Phase-1 routes hidden until later phase |
| Permissions seed | Repair/Borrow/Inventory groups are removed by Phase 1 seed | Existing production DB must run seed or manual cleanup | Verify role permissions after bootstrap |
| Docker verification | Docker daemon was unavailable in local verification | Fresh DB smoke test not yet signed off | Run on Docker Desktop and record result |
| Backup/restore | Backup docs exist but restore has not been verified in this environment | Recovery process may fail during incident | Perform disposable restore test |
| Token storage | LocalStorage mode remains default | Higher XSS impact if frontend is compromised | Use cookie mode after CSRF/CORS validation or accept as documented Phase 1 risk |

## Recommended Implementation Order

1. Database/deploy hardening: migration-only production path, seeder gating, fresh DB smoke test.
2. Security hardening: default admin rotation, role matrix, permission seed cleanup, auth/attachment/PDF denial tests.
3. Phase 1 UI lock: hide non-Phase-1 modules from navigation and route exposure.
4. Operations readiness: backup/restore verification, monitoring checks, production env checklist.
5. Final QA: full Phase 1 manual flow and automated build/test gate.

## Suggested Command To Start Implementation

```text
อ่าน docs/TASK.md แล้วดำเนินการ Priority 1 สำหรับ Phase 1 Production Deploy Readiness โดยเริ่มจาก database/deploy hardening: production migration path, seeder gating, fresh DB smoke test docs/result, และ default admin production bootstrap ห้ามเพิ่มโมดูลใหม่
```
