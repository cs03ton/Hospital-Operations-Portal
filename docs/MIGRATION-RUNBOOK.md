# Migration Runbook

This runbook describes the standard EF Core migration flow for Hospital Operations Portal.

## Current Migration Status

Latest migration file:

```text
20260617010548_Phase21LeaveApprovalAdvanced
```

The migration adds Phase 2.1 Leave Approval Advanced schema changes.

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

## Verify After Migration

Check that the latest migration is applied:

```sql
SELECT "MigrationId", "ProductVersion"
FROM public."__EFMigrationsHistory"
ORDER BY "MigrationId" DESC;
```

Expected latest migration:

```text
20260617010548_Phase21LeaveApprovalAdvanced
```

Check Phase 2.1 tables:

```sql
SELECT schemaname, tablename, tableowner
FROM pg_tables
WHERE schemaname = 'public'
  AND tablename IN (
    'approval_chains',
    'approval_chain_steps',
    'leave_balance_adjustments',
    'leave_holidays',
    'line_delivery_logs'
  )
ORDER BY tablename;
```

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

After the owner fix and migration succeed, the recommended next work is:

- Real LINE sender worker
- Virus scanning for attachments
- Approval delegation and escalation
- Leave reports and export
