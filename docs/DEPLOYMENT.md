# Deployment

## Docker Compose Services

Current deployment stack:

- `postgres`
- `backend`
- `frontend`
- `nginx`
- `clamav`

## ClamAV

The `clamav` service is included for real file scanning.

Important environment:

```text
FILE_SCAN_ENABLED=true
FILE_SCAN_PROVIDER=ClamAV
FILE_SCAN_FAIL_CLOSED=true
CLAMAV_HOST=clamav
CLAMAV_PORT=3310
```

The backend waits for the ClamAV health check before startup when running the full compose stack.

## Cookie Auth Mode

Production cookie mode:

```text
AUTH_TOKEN_STORAGE_MODE=Cookie
AUTH_COOKIE_SECURE=true
AUTH_COOKIE_SAMESITE=Lax
AUTH_COOKIE_CSRF_ENABLED=true
CORS_ALLOW_CREDENTIALS=true
VITE_AUTH_TOKEN_STORAGE_MODE=cookie
```

Keep `Cors__AllowedOrigins` restricted to trusted frontend URLs.

## Fresh Database Verification

Use the disposable smoke test in `docs/TESTING.md` before production migration rehearsals.

The smoke test:

1. Starts a new PostgreSQL container with a disposable volume.
2. Runs EF Core migrations.
3. Starts backend.
4. Verifies `/healthz`.
5. Verifies critical tables.
6. Verifies default development admin login.

## Production Migration Path

Phase 1 production deploy uses EF Core migrations as the database source of truth.

Do not initialize a persistent production database from `database/schema.sql` or `database/seed.sql` and then run EF Core migrations on top of it. Those SQL files are reference/bootstrap artifacts; production schema changes must go through EF Core migrations.

Production release sequence:

```powershell
$env:ConnectionStrings__DefaultConnection='Host=<db-host>;Port=5432;Database=hop_db;Username=hop_user;Password=<password>'
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

Backend startup seeding is controlled by:

```text
Database__SeedOnStartup=false
```

Keep it disabled by default in production. Enable it only for an explicit, audited bootstrap step after migrations are applied.

## Production Admin Bootstrap

The development admin account must not be used in production.

Production default:

```text
Seed__CreateDefaultAdmin=false
```

If the first production administrator must be bootstrapped by the application, use a temporary strong password and rotate it immediately:

```text
Database__SeedOnStartup=true
Seed__CreateDefaultAdmin=true
Seed__AdminUsername=<admin-username>
Seed__AdminPassword=<strong-temporary-password>
Seed__AdminFullName=<admin-full-name>
Seed__AdminEmployeeCode=<employee-code>
```

After the first admin login:

1. Change the password.
2. Set `Seed__CreateDefaultAdmin=false`.
3. Restart the backend.
4. Confirm the admin can still log in.

Rehearsal commands are documented in `docs/TESTING.md`.

## Phase 1 UI Exposure

Phase 1 deployment must expose only:

- Dashboard
- User Management
- Department Management
- Role/Permission Management
- Audit Log
- Leave Management
- Leave report page

The frontend navigation and route table must not expose:

- Repair Management
- Asset Borrowing
- Vehicle Booking
- Meeting Room Booking
- Material Request
- Inventory Management
- Generic Reports
- Generic Administration placeholder pages

Backend currently has no real controllers for the hidden future modules. Their placeholder frontend routes must remain disabled until those modules are implemented and approved for a later phase.

## Phase 1 Deploy Checklist

Use `docs/PRODUCTION-CHECKLIST.md` for final sign-off.

## Production Notes

- Do not use the default development admin in production.
- Do not use production database for tests.
- Store secrets in the deployment secret manager, not `.env` committed files.
- Back up PostgreSQL and uploaded file storage before migrations.
- Enable ClamAV fail-closed mode for production uploads.
- Use `ASPNETCORE_ENVIRONMENT=Production`.
- Use an application database user that is not a PostgreSQL superuser.
- Restrict CORS origins to the production frontend URL.
