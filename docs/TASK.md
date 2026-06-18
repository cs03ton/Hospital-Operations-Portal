# TASK: Leave QA Stabilization Phase

Status: Implemented for current stabilization scope.

Goal: ทำให้ Leave Operations & Reliability ผ่าน QA ก่อนเพิ่มฟีเจอร์ใหม่ โดยโฟกัส bug, security, tests, CI, database alignment และ documentation

## Completed

1. Leave report export hardening
- Excel export encodes user-controlled text.
- Formula-like values are prefixed before export.
- PDF export paginates leave rows instead of truncating at 24 rows.

2. Test coverage
- Added tests for report export escaping and PDF pagination.
- Added tests for LINE retry success and missing-token failure.
- Added tests for file upload allowed extension, disallowed extension, and size limit.
- Added test for unrelated user attachment download denial.
- Added tests for login rate limit lockout and reset behavior.

3. Frontend stabilization
- Approval Delegation actions now use mutation error handling and query invalidation.
- Create/delete/manage actions are wrapped with frontend `PermissionGuard` for UX.
- Leave Calendar supports Thai month selector and Buddhist year selector.

4. Security stabilization
- Added configurable in-memory login rate limit and lockout.
- Added audit event `Auth.LoginLocked` for locked login attempts.

5. CI and database readiness
- Added PostgreSQL service to GitHub Actions backend job.
- Added EF Core migration smoke test in CI.
- Aligned `database/schema.sql` DateTime columns to `TIMESTAMPTZ`.

6. Documentation
- Updated authentication, testing, CI/CD, database migration, owner fix, approval workflow, leave calendar, leave reports, and admin module documentation.

## Verification

```powershell
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
dotnet build backend/Hop.Api/Hop.Api.csproj
cd frontend
npm run build
```

## Fresh Docker Database Check

Use a disposable compose project:

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

## Recommended Next TASK

1. Run full API E2E tests against a live disposable PostgreSQL database.
2. Replace HTML `.xls` export with native `.xlsx` export when a spreadsheet library is approved.
3. Add real ClamAV implementation behind `IFileScanningService`.
4. Add LINE retry management UI and operational monitoring.
5. Move session tokens from local storage to a more hardened browser storage strategy.
