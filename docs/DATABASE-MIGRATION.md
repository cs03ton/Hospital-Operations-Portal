# Database Migration

## Source Of Truth

EF Core migrations and `AppDbContext` are the source of truth.

`database/schema.sql` is maintained as a reference/bootstrap artifact and must follow the current EF Core model. Production deployment must use EF Core migrations, not PostgreSQL init scripts, as the schema creation and upgrade path.

Current latest migration:

```text
20260618075042_LeaveOperationsReliability
```

## Production Database Initialization

Production flow:

1. Create PostgreSQL database and application user.
2. Confirm the application user owns the database objects it will migrate.
3. Run EF Core migrations from a trusted deployment workstation or CI/CD release job.
4. Start the backend with `Database__SeedOnStartup=false` unless an explicit seed/bootstrap step is planned.
5. If seed/bootstrap is needed, enable it only after migrations are applied.

```powershell
$env:ConnectionStrings__DefaultConnection='Host=<db-host>;Port=5432;Database=hop_db;Username=hop_user;Password=<password>'
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

Do not run both `database/schema.sql` init scripts and EF Core migrations against the same persistent production database.

## Fresh Docker Database Smoke Test

For a disposable verification install, use the full procedure in `docs/TESTING.md`.

For a local compose database reset, remove the old database volume first.

```powershell
docker compose down
docker volume rm hospital-operations-portal_hop_postgres_data
docker compose up -d postgres
```

Then run EF Core migrations before starting the backend:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

## EF Migration Update

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

## Startup Seeding

The backend seeder no longer creates schema.

Use:

```text
Database__SeedOnStartup=true
```

only after EF Core migrations have been applied.

Production default should be:

```text
Database__SeedOnStartup=false
Seed__CreateDefaultAdmin=false
```

If a bootstrap admin must be created, set an explicit non-default password through environment variables:

```text
Seed__CreateDefaultAdmin=true
Seed__AdminUsername=<admin-username>
Seed__AdminPassword=<strong-temporary-password>
Seed__AdminFullName=<admin-full-name>
Seed__AdminEmployeeCode=<employee-code>
```

Disable bootstrap admin creation again after the first successful login and password rotation.

## Current Schema Coverage

Fresh schema includes:

- `approval_chains`
- `approval_chain_steps`
- `line_delivery_logs`
- `leave_holidays`
- `leave_types`
- `leave_requests`
- `leave_approvals`
- `leave_attachments`
- `leave_balances`
- `leave_balance_adjustments`
- `approval_delegations`
- `approval_escalation_rules`

`schema.sql` uses `TIMESTAMPTZ` for DateTime columns to match the Npgsql EF Core migration type `timestamp with time zone`.
