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
- Phase 1 critical leave approval flow: Head approval moves to Director, final approval clears current approver and deducts used balance
- Phase 1 reject/cancel flow: pending balance is returned and approval notification target is cleared
- Phase 1 approval authorization: non-current approver cannot approve before their step
- Leave overlap validation
- Leave remaining balance validation
- PDF byte generation
- Leave report Excel export escaping
- Native `.xlsx` workbook generation
- Leave report PDF pagination
- LINE retry delivery success and missing-token failure
- ClamAV clean, infected, and unavailable scanner behavior
- Leave attachment access denial for unrelated users
- Leave upload extension and size validation
- Optional PostgreSQL-backed API E2E tests when `HOP_E2E_CONNECTION_STRING` is set

## Frontend

Run:

```powershell
cd frontend
npm ci
npm run build
```

This verifies TypeScript compile and Vite production build.

## Fresh Docker Database Smoke Test

Requires Docker Desktop.

Recommended disposable Docker check for Phase 1. This uses a disposable container and volume only.

```powershell
$env:POSTGRES_DB='hop_qa'
$env:POSTGRES_USER='hop_qa'
$env:POSTGRES_PASSWORD='hop_qa_password'
$env:POSTGRES_PORT='55432'
$env:JWT_SECRET='qa-test-secret-key-that-is-long-enough-32'
$env:ConnectionStrings__DefaultConnection='Host=localhost;Port=55432;Database=hop_qa;Username=hop_qa;Password=hop_qa_password'
$env:Database__SeedOnStartup='true'
$env:Seed__CreateDefaultAdmin='true'
$env:Seed__AdminUsername='admin'
$env:Seed__AdminPassword='Admin@1234'
$env:Seed__AdminFullName='Default Administrator'
$env:Seed__AdminEmployeeCode='ADMIN'

docker rm -f hop-qa-postgres 2>$null
docker volume rm hop_qa_postgres_data 2>$null
docker volume create hop_qa_postgres_data
docker run -d --name hop-qa-postgres -e POSTGRES_DB=hop_qa -e POSTGRES_USER=hop_qa -e POSTGRES_PASSWORD=hop_qa_password -p 55432:5432 -v hop_qa_postgres_data:/var/lib/postgresql/data postgres:16

docker exec hop-qa-postgres pg_isready -U hop_qa -d hop_qa
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj

# Backend startup runs DevelopmentDataSeeder only because Database__SeedOnStartup=true.
# The seeder no longer creates schema; EF Core migrations above are required first.
$backend = Start-Process -FilePath dotnet -ArgumentList "run --project backend/Hop.Api/Hop.Api.csproj --urls http://localhost:55000" -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 8

curl.exe -f http://localhost:55000/healthz
docker exec hop-qa-postgres psql -U hop_qa -d hop_qa -c "SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_name IN ('users','roles','permissions','leave_types','leave_requests','leave_approvals','leave_attachments','leave_balances','approval_chains','approval_chain_steps','approval_delegations','approval_escalation_rules','line_delivery_logs') ORDER BY table_name;"
curl.exe -s -X POST http://localhost:55000/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"admin\",\"password\":\"Admin@1234\"}"

Stop-Process -Id $backend.Id
docker rm -f hop-qa-postgres
docker volume rm hop_qa_postgres_data
```

Expected result:

- PostgreSQL container starts with a disposable volume.
- EF Core migrations apply successfully.
- Backend starts only after migrations are applied.
- `DevelopmentDataSeeder` creates roles, Phase 1 permissions, leave types, and the local QA admin because `Database__SeedOnStartup=true`.
- `/healthz` returns success.
- Critical Phase 1 tables exist.
- QA admin login succeeds.

## Production Admin Bootstrap Rehearsal

Run this only against a disposable QA database or a controlled production bootstrap window. Do not use the development password in production.

Bootstrap with explicit production admin credentials:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Production'
$env:ConnectionStrings__DefaultConnection='Host=<db-host>;Port=5432;Database=hop_db;Username=hop_user;Password=<password>'
$env:Jwt__Key='<strong-jwt-secret-at-least-32-characters>'
$env:Database__SeedOnStartup='true'
$env:Seed__CreateDefaultAdmin='true'
$env:Seed__AdminUsername='<production-admin-username>'
$env:Seed__AdminPassword='<strong-temporary-password>'
$env:Seed__AdminFullName='<production-admin-full-name>'
$env:Seed__AdminEmployeeCode='<employee-code>'

dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
$backend = Start-Process -FilePath dotnet -ArgumentList "run --project backend/Hop.Api/Hop.Api.csproj --urls http://localhost:55000" -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 8

curl.exe -s -X POST http://localhost:55000/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"<production-admin-username>\",\"password\":\"<strong-temporary-password>\"}"
Stop-Process -Id $backend.Id
```

Then disable bootstrap and verify the same production admin can still log in:

```powershell
$env:Database__SeedOnStartup='false'
$env:Seed__CreateDefaultAdmin='false'

$backend = Start-Process -FilePath dotnet -ArgumentList "run --project backend/Hop.Api/Hop.Api.csproj --urls http://localhost:55000" -WindowStyle Hidden -PassThru
Start-Sleep -Seconds 8

curl.exe -s -X POST http://localhost:55000/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"<production-admin-username>\",\"password\":\"<strong-temporary-password>\"}"
curl.exe -s -X POST http://localhost:55000/api/auth/login -H "Content-Type: application/json" -d "{\"username\":\"admin\",\"password\":\"Admin@1234\"}"
Stop-Process -Id $backend.Id
```

Expected result:

- Production admin login succeeds before and after `Seed__CreateDefaultAdmin=false`.
- Development admin login fails unless it already exists from an unsafe previous seed.
- If the development admin exists in a production database, disable or rotate it before deploy.

## PostgreSQL E2E API Tests

Run against a test database only:

```powershell
$env:HOP_E2E_CONNECTION_STRING='Host=localhost;Port=55432;Database=hop_qa;Username=hop_qa;Password=hop_qa_password'
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj --filter FullyQualifiedName~E2E
```

The E2E fixture resets the configured database with EF Core migrations and development seed data before running. If `HOP_E2E_CONNECTION_STRING` is not set, the E2E test exits without touching any database.

## Phase 1 Pilot Critical Test Suite

Automated backend gate:

```powershell
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
```

If local backend/frontend dev servers lock normal `bin` or `obj` outputs, use an isolated output folder:

```powershell
dotnet build backend/Hop.Api.Tests/Hop.Api.Tests.csproj -o tmp/hopapi-tests-phase1
dotnet vstest tmp/hopapi-tests-phase1/Hop.Api.Tests.dll
```

Frontend production gate:

```powershell
cd frontend
npm run build
```

Optional browser E2E gate:

```powershell
cd frontend
npm run e2e:phase1
```

Manual pilot flow to verify with screenshots:

1. Login as `staff01`.
2. Create a leave request with enough balance.
3. Submit the request.
4. Login as `head01` and verify Notification Bell, Dashboard pending widget, `/leave/pending-approvals`, and LINE delivery log.
5. Approve as `head01`.
6. Login as `director01` and verify the pending item moved to Director only.
7. Approve as `director01`.
8. Verify request status is `อนุมัติแล้ว`, `CurrentApproverId` is empty, leave balance `usedDays` increased, and PDF download works.
9. Repeat a separate request for reject flow and confirm pending notification disappears and balance is not deducted.
10. Repeat a separate request for cancel flow and confirm current approver notification disappears and balance is not deducted.
11. Verify Staff cannot see other users' leave requests.
12. Verify Head cannot approve before the current approval step or approve another department's request.
13. Verify Admin/SuperAdmin cannot create leave through UI or API.
14. Verify LINE disabled or LINE API failure does not break leave workflow and writes delivery log status.

Record pilot evidence in:

```text
docs/qa/PHASE1-PILOT-TEST-REPORT.md
docs/qa/screenshots/phase1/
```

## Fresh DB Smoke Test Result

Latest local verification attempt:

```text
Date: 2026-06-18
Result: Blocked on local Docker Desktop daemon
Docker client: available
Docker daemon: unavailable, dockerDesktopLinuxEngine pipe not found
```

The smoke test command sequence above is the Phase 1 sign-off procedure. Run it on a machine with Docker Desktop running before production deploy and record the result here.
