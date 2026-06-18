# Phase 1 Production Deploy Checklist

Use this checklist before opening Hospital Operations Portal to staff.

## Scope Lock

Phase 1 opens only:

- Dashboard for leave metrics
- User Management
- Department Management
- Role/Permission Management
- Audit Log
- Leave Management
- Leave approval workflow
- Leave balances
- Leave attachment upload/download
- Leave PDF export
- Leave LINE notification delivery or fallback logging

Do not expose Repair Management, Asset Borrowing, Vehicle Booking, Meeting Room Booking, Material Request, Inventory Management, or generic Reports in Phase 1.

## Infrastructure

- [ ] Docker Desktop or Docker Engine is running.
- [ ] PostgreSQL runs with a disposable test volume for smoke test.
- [ ] Production PostgreSQL application user is not a superuser.
- [ ] Uploaded file storage is mounted outside the backend container.
- [ ] Backup target path is available.
- [ ] LINE token and secrets are stored outside source code.

## Database

- [ ] Fresh DB smoke test in `docs/TESTING.md` passes.
- [ ] EF Core migrations run successfully.
- [ ] `database/schema.sql` is treated as reference/bootstrap only.
- [ ] Production deploy does not run PostgreSQL init SQL and EF migrations against the same persistent database.
- [ ] Critical tables exist: `users`, `roles`, `permissions`, `departments`, `leave_types`, `leave_requests`, `leave_approvals`, `leave_attachments`, `leave_balances`, `line_delivery_logs`, `audit_logs`.
- [ ] `Database__SeedOnStartup=false` for normal production startup.

## Production Admin

- [ ] Production admin bootstrap rehearsal succeeds.
- [ ] Production admin can log in with real username.
- [ ] `Seed__CreateDefaultAdmin=false` after bootstrap.
- [ ] Development admin `admin / Admin@1234` is not usable in production.
- [ ] Admin password is rotated after first login.

## Security

- [ ] `Jwt__Key` or `JWT_SECRET` is strong and not committed.
- [ ] CORS origins include only trusted frontend URLs.
- [ ] Login lockout is enabled.
- [ ] Refresh token reuse detection is enabled.
- [ ] Cookie mode, if used, has CSRF enabled.
- [ ] Users cannot download another user's leave attachment.
- [ ] Users cannot download another user's leave PDF.
- [ ] Permission denied attempts are logged.

## Phase 1 UI

- [ ] Sidebar shows only Phase 1 items.
- [ ] Hidden module URLs redirect to dashboard or unauthorized flow.
- [ ] Dashboard shows only leave-related cards.
- [ ] UI text is Thai.
- [ ] Leave report page, if enabled, shows only leave reports.

## Leave Flow

- [ ] Create leave type.
- [ ] Create leave balance.
- [ ] Create approval chain.
- [ ] Create holiday.
- [ ] User creates leave request.
- [ ] Overlap validation works.
- [ ] Required attachment validation works.
- [ ] Attachment upload/download works.
- [ ] Submit works.
- [ ] Approve works.
- [ ] Reject works.
- [ ] Leave balance pending/used values update correctly.
- [ ] PDF leave form downloads successfully.
- [ ] LINE notification sends or writes failed/disabled delivery log without breaking workflow.

## Audit And Monitoring

- [ ] Login success/failure/lockout events are visible.
- [ ] Leave create/submit/approve/reject/cancel events are visible.
- [ ] Attachment upload/download/failure events are visible.
- [ ] PDF generation event is visible.
- [ ] Audit export is restricted.
- [ ] Security summary endpoint is restricted to admin permissions.

## Backup / Restore

- [ ] `pg_dump` command is ready.
- [ ] `pg_restore` rehearsal into a disposable database passes.
- [ ] Uploaded file storage backup command is ready.
- [ ] Restore runbook includes database and uploaded files.

## Final Gate

- [ ] `dotnet build backend/Hop.Api/Hop.Api.csproj` passes.
- [ ] `dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj` passes.
- [ ] PostgreSQL E2E tests pass with `HOP_E2E_CONNECTION_STRING`.
- [ ] `npm run build` passes.
- [ ] `docker compose config --quiet` passes with production-like env.
- [ ] Pilot roles and permissions are reviewed.
