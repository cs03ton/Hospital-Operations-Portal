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
