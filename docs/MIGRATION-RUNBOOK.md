# Migration Runbook

This runbook describes the standard EF Core and DBeaver migration flow for Hospital Operations Portal.

## Production DBeaver Bundle

Use these files in order:

1. Back up the production database and verify that the backup can be read.
2. Open `deploy/sql/01-prd-schema-migrations.sql` in DBeaver, enable **Stop on error**, and execute the whole script.
3. Confirm that the latest row in `__EFMigrationsHistory` is `20260909090000_CorrectFleetRolePermissions`.
4. Open `deploy/sql/02-prd-master-data.sql` and execute the whole script.
5. Review the vehicle and driver result sets returned at the end of the script.
6. Validate `.env.production` with `deploy/00-check-env.sh` and `docker compose config`.
7. Restart the application and run `deploy/05-smoke-e2e.sh`.

Both SQL files are idempotent. The schema bundle executes only migrations absent from
`__EFMigrationsHistory`; the master-data bundle inserts missing records without overwriting
existing operational vehicle or user data. Do not run the legacy `database/seed.sql` in
production because it contains development bootstrap behavior.

## Current Migration Status

Latest migration file:

```text
20260909090000_CorrectFleetRolePermissions
```

The migration includes the current Leave Operations and Reliability schema.

If PostgreSQL reports `must be owner of table leave_approvals`, complete `docs/DATABASE-OWNER-FIX.md` before continuing.

## Pre-Checks

Restore local EF tool:

```bash
dotnet tool restore
```

Check current database owner status:

```sql
SELECT schemaname, tablename, tableowner
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;
```

Check current privileges:

```sql
SELECT grantee, table_schema, table_name, privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'public'
ORDER BY table_name, grantee;
```

Check applied migrations:

```sql
SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
ORDER BY "MigrationId" DESC;
```

## Run Migration

From the project root:

```bash
dotnet tool run dotnet-ef database update --project backend\Hop.Api\Hop.Api.csproj --startup-project backend\Hop.Api\Hop.Api.csproj
```

For production, keep startup seeding disabled while running migrations:

```text
Database__SeedOnStartup=false
Seed__CreateDefaultAdmin=false
```

## Verify After Migration

Check that the latest migration is applied:

```sql
SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
ORDER BY "MigrationId" DESC;
```

Expected latest migration:

```text
20260909090000_CorrectFleetRolePermissions
```

Check Phase 1 deployment-critical tables:

```sql
SELECT schemaname, tablename, tableowner
FROM pg_tables
WHERE schemaname = 'public'
  AND tablename IN (
    'approval_chains',
    'approval_chain_steps',
    'approval_delegations',
    'approval_escalation_rules',
    'audit_logs',
    'departments',
    'leave_balance_adjustments',
    'leave_balances',
    'leave_holidays',
    'leave_requests',
    'leave_approvals',
    'leave_attachments',
    'leave_types',
    'line_delivery_logs',
    'permissions',
    'refresh_tokens',
    'role_permissions',
    'roles',
    'user_roles',
    'users'
  )
ORDER BY tablename;
```

## Startup Seed / Bootstrap

The application seeder no longer creates database schema. Run EF Core migrations first.

For local development or disposable QA only:

```text
Database__SeedOnStartup=true
Seed__CreateDefaultAdmin=true
Seed__AdminUsername=admin
Seed__AdminPassword=<one-time-strong-password>
```

For production default:

```text
Database__SeedOnStartup=false
Seed__CreateDefaultAdmin=false
```

If an initial production admin must be bootstrapped, set a unique strong temporary password through the environment and rotate it immediately after first login. Never store that password in SQL, source control, or this runbook.

Check application health:

```bash
curl http://localhost:5000/healthz
```

PowerShell:

```powershell
Invoke-RestMethod -Uri "http://localhost:5000/healthz"
```

## Troubleshooting

### Owner Error

Symptom:

```text
ERROR: must be owner of table leave_approvals
```

Fix:

1. Ask DBA or PostgreSQL superuser to run `docs/DATABASE-OWNER-FIX.md`.
2. Re-run the migration command.

### Missing dotnet-ef

Symptom:

```text
Run "dotnet tool restore" to make the "dotnet-ef" command available.
```

Fix:

```bash
dotnet tool restore
```

### Locked Build Output

If `dotnet build` cannot copy `Hop.Api.dll` or `Hop.Api.exe`, stop the running backend process and rerun the command.

## Next Phase Preparation

After the owner fix and migration succeed, the recommended next Phase 1 work is:

- Run the fresh DB smoke test in `docs/TESTING.md`
- Verify production admin bootstrap
- Verify login, user management, permissions, and leave workflow
- Verify backup and restore
