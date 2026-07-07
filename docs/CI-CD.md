# CI/CD

GitHub Actions workflow:

```text
.github/workflows/ci.yml
```

## Jobs

- Backend build and test
- PostgreSQL-backed E2E API tests
- PostgreSQL migration smoke test
- Frontend install and build
- Frontend dist readiness scan
- Production readiness gate

## Backend

```text
dotnet restore backend/Hop.Api/Hop.Api.csproj
dotnet build backend/Hop.Api/Hop.Api.csproj --configuration Release --no-restore
HOP_E2E_CONNECTION_STRING=Host=localhost;Port=5432;Database=hop_ci;Username=hop_ci;Password=hop_ci_password dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj --configuration Release
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

The backend test job starts a temporary PostgreSQL service with CI-only credentials. The E2E fixture resets that database before running API flow tests.

## Frontend

```text
npm ci
npm run build
```

CI does not require production secrets.

Frontend production build uses same-origin API defaults in CI. If a separate API host is required for deployment, set `VITE_API_BASE_URL` explicitly in the protected deployment environment.

The frontend job scans `frontend/dist` after build and fails if the bundle contains localhost API endpoints, secret marker names, or default development credentials.

## Production Readiness Gate

The `production-readiness` job checks:

1. Forbidden secret/default credential markers in source and example env files
2. Shell syntax for deploy and backup scripts
3. `docker compose -f docker-compose.prod.yml config` using CI-only dummy values
4. Production compose output does not contain forbidden development markers

This gate is not a replacement for staging deployment, but it prevents common readiness regressions before merge.
