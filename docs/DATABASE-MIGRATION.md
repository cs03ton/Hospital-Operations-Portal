# Database Migration

## Source Of Truth

EF Core migrations and `AppDbContext` are the source of truth.

`database/schema.sql` is maintained for fresh Docker initialization and must follow the current EF Core model.

Current latest migration:

```text
20260618075042_LeaveOperationsReliability
```

## Fresh Docker Database

For a truly fresh install, remove the old database volume first.

```powershell
docker compose down
docker volume rm hospital-operations-portal_hop_postgres_data
docker compose up -d postgres
```

Then verify tables:

```powershell
docker compose exec postgres psql -U $env:POSTGRES_USER -d $env:POSTGRES_DB -c "\dt"
```

## EF Migration Update

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

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
