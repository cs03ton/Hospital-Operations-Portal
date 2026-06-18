# E2E API Testing

E2E API tests live in:

```text
backend/Hop.Api.Tests/E2E
```

## Safety Rule

Never point E2E tests at production.

The tests run only when this environment variable is set:

```text
HOP_E2E_CONNECTION_STRING
```

If it is missing, the E2E test exits without touching a database.

## Local PostgreSQL Test Database

```powershell
docker rm -f hop-e2e-postgres 2>$null
docker volume rm hop_e2e_postgres_data 2>$null
docker volume create hop_e2e_postgres_data
docker run -d --name hop-e2e-postgres `
  -e POSTGRES_DB=hop_e2e `
  -e POSTGRES_USER=hop_e2e `
  -e POSTGRES_PASSWORD=hop_e2e_password `
  -p 55433:5432 `
  -v hop_e2e_postgres_data:/var/lib/postgresql/data `
  postgres:16

$env:HOP_E2E_CONNECTION_STRING='Host=localhost;Port=55433;Database=hop_e2e;Username=hop_e2e;Password=hop_e2e_password'
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj --filter FullyQualifiedName~E2E

docker rm -f hop-e2e-postgres
docker volume rm hop_e2e_postgres_data
```

## Covered Flow

- Auth login
- Refresh token
- Permission enforcement
- Create leave request
- Submit leave request
- Approve leave request
- Reject leave request
- Leave balance calculation
- Attachment upload/download access control
- Leave PDF download
- Leave report `.xlsx` export
- LINE delivery log fallback when LINE is disabled

## CI

GitHub Actions starts a PostgreSQL service and sets:

```text
HOP_E2E_CONNECTION_STRING=Host=localhost;Port=5432;Database=hop_ci;Username=hop_ci;Password=hop_ci_password
```

The E2E fixture resets the configured database by running EF Core migrations and development seed data before test execution.
