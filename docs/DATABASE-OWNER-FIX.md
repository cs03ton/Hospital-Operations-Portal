# Database Owner Fix

This runbook fixes PostgreSQL table owner and permission issues that can block EF Core migrations.

## Problem

EF Core migrations through `20260618075042_LeaveOperationsReliability` need to alter existing Leave Module tables.

The application connects as `hop_user`, but some existing Leave Module tables were created with owner `postgres`.

`hop_user` can read and write the tables because it has DML privileges, but PostgreSQL requires the table owner or a superuser for schema changes such as:

- `ALTER TABLE`
- Adding columns
- Adding foreign keys
- Creating indexes on existing tables

When EF Core tries to apply the migration, PostgreSQL returns:

```text
ERROR: must be owner of table leave_approvals
```

## Who Should Run This

Only a PostgreSQL DBA, database owner, or PostgreSQL superuser should run the owner fix.

Do not run these commands from the application user unless that user already has ownership or elevated database administration rights.

## Check Current Owners

```sql
SELECT schemaname, tablename, tableowner
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;
```

Focused Leave Module check:

```sql
SELECT schemaname, tablename, tableowner
FROM pg_tables
WHERE schemaname = 'public'
  AND tablename IN (
    'leave_types',
    'leave_balances',
    'leave_requests',
    'leave_attachments',
    'leave_approvals',
    'approval_chains',
    'approval_chain_steps',
    'leave_balance_adjustments',
    'leave_holidays',
    'line_delivery_logs',
    'approval_delegations',
    'approval_escalation_rules'
  )
ORDER BY tablename;
```

Expected owner for application-managed tables:

```text
hop_user
```

## Check Privileges

```sql
SELECT grantee, table_schema, table_name, privilege_type
FROM information_schema.role_table_grants
WHERE table_schema = 'public'
ORDER BY table_name, grantee;
```

## Fix Owner and Privileges

Run as PostgreSQL superuser or a role that owns the target tables.

```sql
ALTER TABLE public.leave_approvals OWNER TO hop_user;
ALTER TABLE public.leave_requests OWNER TO hop_user;
ALTER TABLE public.leave_attachments OWNER TO hop_user;
ALTER TABLE public.leave_balances OWNER TO hop_user;
ALTER TABLE public.leave_types OWNER TO hop_user;

ALTER TABLE IF EXISTS public.approval_chains OWNER TO hop_user;
ALTER TABLE IF EXISTS public.approval_chain_steps OWNER TO hop_user;
ALTER TABLE IF EXISTS public.leave_balance_adjustments OWNER TO hop_user;
ALTER TABLE IF EXISTS public.leave_holidays OWNER TO hop_user;
ALTER TABLE IF EXISTS public.line_delivery_logs OWNER TO hop_user;
ALTER TABLE IF EXISTS public.approval_delegations OWNER TO hop_user;
ALTER TABLE IF EXISTS public.approval_escalation_rules OWNER TO hop_user;

GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO hop_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO hop_user;
```

## Recommended psql Pattern

Do not write passwords into command history or documentation.

Use a secure prompt:

```bash
psql -h <db-host> -U <superuser-or-owner> -d hop_db
```

Or use an environment variable only for the current shell session:

```bash
export PGPASSWORD="<database-password>"
psql -h <db-host> -U <superuser-or-owner> -d hop_db
unset PGPASSWORD
```

PowerShell:

```powershell
$env:PGPASSWORD="<database-password>"
psql -h <db-host> -U <superuser-or-owner> -d hop_db
Remove-Item Env:\PGPASSWORD
```

## Run Migration After Owner Fix

From the project root:

```bash
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend\Hop.Api\Hop.Api.csproj --startup-project backend\Hop.Api\Hop.Api.csproj
```

## Verify Migration Success

Check latest EF migration:

```sql
SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
ORDER BY "MigrationId" DESC;
```

Expected latest migration:

```text
20260618075042_LeaveOperationsReliability
```

Check Leave reliability tables:

```sql
SELECT schemaname, tablename, tableowner
FROM pg_tables
WHERE schemaname = 'public'
  AND tablename IN (
    'approval_chains',
    'approval_chain_steps',
    'leave_balance_adjustments',
    'leave_holidays',
    'line_delivery_logs',
    'approval_delegations',
    'approval_escalation_rules'
  )
ORDER BY tablename;
```

Check new columns on `leave_approvals`:

```sql
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'leave_approvals'
  AND column_name IN (
    'approval_chain_id',
    'approval_chain_step_id',
    'step_name',
    'required_permission_code'
  )
ORDER BY column_name;
```

## Basic Rollback

If the migration fails before completion, EF Core usually rolls back the failed transaction automatically.

If the migration succeeds but must be reverted, run:

```bash
dotnet tool run dotnet-ef database update 20260617010548_Phase21LeaveApprovalAdvanced --project backend\Hop.Api\Hop.Api.csproj --startup-project backend\Hop.Api\Hop.Api.csproj
```

Before rollback in shared or production environments:

1. Take a database backup.
2. Confirm no users are actively using Leave Approval Advanced features.
3. Coordinate downtime with the system owner.
4. Verify `__EFMigrationsHistory` after rollback.

Backup reference: `docs/BACKUP-STRATEGY.md`.
