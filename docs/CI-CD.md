# CI/CD

GitHub Actions workflow:

```text
.github/workflows/ci.yml
```

## Jobs

- Backend build and test
- PostgreSQL migration smoke test
- Frontend install and build

## Backend

```text
dotnet restore backend/Hop.Api/Hop.Api.csproj
dotnet build backend/Hop.Api/Hop.Api.csproj --configuration Release --no-restore
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj --configuration Release
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

The migration smoke test runs against a temporary PostgreSQL service with CI-only credentials.

## Frontend

```text
npm ci
npm run build
```

CI does not require production secrets.
