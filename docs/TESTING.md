# Testing

## Backend

Test project:

```text
backend/Hop.Api.Tests
```

Run:

```powershell
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
```

Current coverage includes:

- JWT access token generation
- Permission policy attribute
- Login rate limit lockout behavior
- Leave overlap validation
- Leave remaining balance validation
- PDF byte generation
- Leave report Excel export escaping
- Leave report PDF pagination
- LINE retry delivery success and missing-token failure
- Leave attachment access denial for unrelated users
- Leave upload extension and size validation

## Frontend

Run:

```powershell
cd frontend
npm ci
npm run build
```

This verifies TypeScript compile and Vite production build.

## Fresh Database

Recommended disposable Docker check:

```powershell
$env:POSTGRES_DB='hop_qa'
$env:POSTGRES_USER='hop_qa'
$env:POSTGRES_PASSWORD='hop_qa_password'
$env:POSTGRES_PORT='55432'
$env:JWT_SECRET='qa-test-secret-key-that-is-long-enough-32'
docker compose -p hop_qa_stabilization up -d postgres
docker compose -p hop_qa_stabilization exec postgres psql -U hop_qa -d hop_qa -c "\dt"
docker compose -p hop_qa_stabilization down -v
```
