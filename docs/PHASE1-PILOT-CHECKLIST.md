# Phase 1 Pilot Checklist

Use this checklist before deploying HOP to the first real department.

## Infrastructure

- [ ] Ubuntu Server or Docker host is ready.
- [ ] Docker Engine and Docker Compose are installed.
- [ ] `docker compose --env-file .env.production -f docker-compose.prod.yml config` passes.
- [ ] PostgreSQL volume `hop_prod_postgres_data` is created.
- [ ] Storage volume `hop_prod_storage` is created.
- [ ] ClamAV service is healthy if file scanning is enabled.
- [ ] Nginx responds on the expected public URL.

## Environment

- [ ] `.env.production` is created from `.env.production.example`.
- [ ] `Jwt__Key` is strong and unique.
- [ ] `POSTGRES_PASSWORD` is strong and unique.
- [ ] `PUBLIC_APP_URL` is the real production URL.
- [ ] `VITE_API_BASE_URL=` is blank for same-origin production deployment because frontend API calls already use `/api/...`.
- [ ] `Seed__CreateDefaultAdmin=false`.
- [ ] `Database__SeedOnStartup=false`.
- [ ] LINE settings are either configured or explicitly disabled.

## Database

- [ ] Backup exists before migration.
- [ ] EF Core migration applies successfully.
- [ ] `__EFMigrationsHistory` contains the latest migration.
- [ ] Critical tables exist: users, roles, permissions, leave_requests, leave_balances, leave_types.
- [ ] Fiscal year balance columns exist:
  - `leave_balances.carried_over_days`
  - `leave_types.allow_carry_over`
  - `leave_types.carry_over_max_days`
  - `leave_types.use_fiscal_year`

## Admin Bootstrap

- [ ] Production admin account exists.
- [ ] Production admin can login.
- [ ] Default development admin is disabled or not created.
- [ ] Admin can manage users, roles, leave types, holidays, approval rules, and balances.

## Leave Management

- [ ] Staff can create leave requests.
- [ ] Staff cannot request leave on weekends or public holidays.
- [ ] Half-day leave consumes 0.5 day.
- [ ] Submit blocks when available balance is not enough.
- [ ] Pending leave is deducted from available balance.
- [ ] Head sees department requests according to permission.
- [ ] Approval queue only shows current approver work.
- [ ] Approve and reject update notification badge.
- [ ] PDF download shows Thai text correctly.
- [ ] Attachments upload and download work.

## Fiscal Year Balance

- [ ] Leave balance page shows fiscal year.
- [ ] Filters work by fiscal year, user, department, and leave type.
- [ ] Rollover creates next fiscal year balances.
- [ ] Carry over is capped at 30 days for configured leave types.
- [ ] Rollover audit event is recorded.

## Backup and Rollback

- [ ] `scripts/backup-postgres.ps1` creates a backup dump.
- [ ] Restore workflow is rehearsed on a non-production database.
- [ ] Uploaded file storage backup is available.
- [ ] Rollback command is documented for the release.

## Go / No-Go

Pilot can start only when all Priority 1 items above pass.
