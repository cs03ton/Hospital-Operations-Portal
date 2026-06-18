# Hospital Operations Portal (HOP)

Hospital Operations Portal is a web-based internal operations platform for hospital teams.
The project follows the setup standard in `docs/SETUP-PROJECT.md`.

## Tech Stack

- Frontend: React, Vite, TypeScript, Material UI, React Router, React Query
- Backend: .NET 9 Web API, Entity Framework Core, PostgreSQL, JWT-ready configuration
- Database: PostgreSQL
- Deployment: Docker Compose, Nginx reverse proxy
- Notification readiness: LINE Messaging API delivery logging and push sender

## Project Structure

```text
hospital-operations-portal/
├── frontend/
├── backend/
├── database/
├── deploy/
├── docs/
├── docker-compose.yml
├── .gitignore
└── README.md
```

## Run Database

```bash
docker compose up -d postgres
```

PostgreSQL defaults:

- Database: `hop_db`
- User: `hop_user`
- Password: configured by `POSTGRES_PASSWORD` in `.env`
- Port: `5432`

EF Core migrations are the database source of truth. `database/schema.sql` is kept as a reference/bootstrap artifact, not the production migration path.

Copy environment settings before running services:

```bash
cp .env.example .env
```

Update `.env` values before any non-local deployment.

## Run Backend

```bash
cd backend/Hop.Api
dotnet restore
dotnet tool restore
dotnet tool run dotnet-ef database update --project Hop.Api.csproj --startup-project Hop.Api.csproj
dotnet run
```

Useful endpoints:

- API root: `http://localhost:5000/api`
- Health check: `http://localhost:5000/healthz`
- Controller health check: `http://localhost:5000/api/health`
- Swagger: available in development mode

## Run Frontend

```bash
cd frontend
npm install
npm run dev
```

Open:

```text
http://localhost:5173
```

## Run With Docker Compose

For a new database, run EF Core migrations before starting the full application stack.

```bash
docker compose up -d postgres
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
docker compose up --build
```

Open:

- Frontend: `http://localhost:5173`
- API: `http://localhost:5000/api`
- Health check: `http://localhost:5000/healthz`
- Nginx gateway: `http://localhost:8080`

## Phase 1 Foundation

Implemented foundation:

- JWT login, refresh token, logout, and current user endpoint
- BCrypt password hashing
- Default development admin user
- Protected backend APIs for users, departments, and roles
- Frontend login page
- Frontend protected routes
- Main layout with sidebar, topbar, dashboard, and logout
- User and department management foundation pages

Phase 1 production deploy opens only:

- Dashboard with leave metrics
- User Management
- Department Management
- Role/Permission Management
- Audit Log
- Leave Management
- Leave report page

Future module menus and routes are hidden for Phase 1:

- Repair Management
- Asset Borrowing
- Vehicle Booking
- Meeting Room Booking
- Material Request
- Inventory Management
- Generic Reports
- Generic Administration placeholder pages

Development default login:

```text
Username: admin
Password: Admin@1234
```

This account is for local development only. Change or remove it before production.

Production must not use the development admin credentials. Keep these defaults unless explicitly bootstrapping a first production admin with a strong temporary password:

```text
Database__SeedOnStartup=false
Seed__CreateDefaultAdmin=false
```

If a bootstrap admin is required, enable it only after migrations are applied and rotate the password immediately:

```text
Database__SeedOnStartup=true
Seed__CreateDefaultAdmin=true
Seed__AdminUsername=<admin-username>
Seed__AdminPassword=<strong-temporary-password>
```

## Initial Modules

The codebase may contain placeholder pages for future modules, but Phase 1 production navigation and routes expose only User Management and Leave Management.

Hidden future modules:

- Asset Borrowing
- Repair Management
- Vehicle Booking
- Meeting Room Booking
- Material Request
- Inventory Management
- Generic Reports
- Generic Administration

These pages are scaffolds only and must remain hidden until a later approved phase.

## Phase 1 API Endpoints

- `POST /api/auth/login`
- `POST /api/auth/refresh-token`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`
- `GET /api/departments`
- `GET /api/departments/{id}`
- `POST /api/departments`
- `PUT /api/departments/{id}`
- `DELETE /api/departments/{id}`
- `GET /api/roles`

See `docs/AUTHENTICATION.md` for auth details.

## Phase 1.1 Admin Foundation

Implemented:

- Create, edit, and deactivate users
- Create, edit, and deactivate departments
- Create, edit, and deactivate custom roles
- Prevent deactivation of system roles through the UI/API
- Role permission assignment
- Permission matrix foundation
- Dashboard summary API integration
- Axios refresh-token auto retry
- EF Core migration: `InitialAdminFoundation`
- Thai UI labels, menus, forms, buttons, validation messages, and dashboard

Useful admin routes:

- `/admin/users`
- `/admin/users/create`
- `/admin/users/{id}/edit`
- `/admin/departments`
- `/admin/departments/create`
- `/admin/departments/{id}/edit`
- `/admin/roles`
- `/admin/roles/{id}/permissions`

EF Core migration commands:

```bash
dotnet tool restore
dotnet tool run dotnet-ef migrations add InitialAdminFoundation --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj --output-dir Migrations
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

## Phase 1.2 Security, Governance and Branding Foundation

Implemented:

- Permission-based backend authorization with `[RequirePermission]`
- Frontend permission guards for routes, menus, and sensitive buttons
- Audit log API and viewer page
- Hospital logo branding on login page, sidebar, header, and favicon
- Logo-based Material UI theme
- Environment variable foundation with `.env` and `.env.example`
- Leave Management database model only, without Leave UI workflow
- EF Core migration: `Phase12SecurityGovernanceBranding`
- Backup and restore documentation

New route:

- `/admin/audit-logs`

New API endpoints:

- `GET /api/audit-logs`
- `GET /api/audit-logs/{id}`

Current migration command:

```bash
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

Backup:

```powershell
$env:PGPASSWORD="<postgres-password>"
pg_dump -h localhost -U postgres -d hop_db -Fc -f database/backup/hop_db_$(Get-Date -Format yyyyMMdd_HHmmss).dump
```

Restore:

```powershell
$env:PGPASSWORD="<postgres-password>"
pg_restore -h localhost -U postgres -d hop_db --clean --if-exists database/backup/<backup-file>.dump
```

More details:

- `docs/PERMISSION-POLICY.md`
- `docs/BRANDING.md`
- `docs/LEAVE-DESIGN.md`
- `docs/BACKUP-STRATEGY.md`

## Phase 2 Leave Workflow Started

Implemented:

- Leave Type Management API and Thai UI
- Leave Request API and Thai UI
- Draft, submit, cancel, approve, and reject workflow
- Leave attachment upload with configured storage path, extension allowlist, and max file size
- My Leave Balance page
- Audit Log CSV export and retention run page
- Session Management API and page
- Refresh token reuse detection
- LINE Messaging delivery logging and push sender
- Leave PDF export
- Dashboard leave metrics from real leave tables
- Backend test project and GitHub Actions CI
- Leave calendar API and page
- LINE retry worker
- File scanning interface for leave attachments
- Approval delegation and escalation foundation
- Leave reports with native `.xlsx` and PDF export
- Login rate limit and lockout foundation
- Optional httpOnly cookie refresh-token mode
- Double-submit CSRF protection for cookie token mode
- ClamAV file scanning adapter behind `IFileScanningService`
- Security monitoring summary API

New frontend routes:

- `/leave`
- `/leave/create`
- `/leave/{id}`
- `/leave/types`
- `/leave/balances`
- `/leave/calendar`
- `/admin/approval-delegations`
- `/reports/leaves`
- `/admin/audit-logs/export`
- `/admin/sessions`

New environment variables:

```text
STORAGE_ROOT_PATH
LEAVE_ATTACHMENT_MAX_FILE_SIZE_MB
LEAVE_ATTACHMENT_ALLOWED_EXTENSIONS
REFRESH_TOKEN_REUSE_DETECTION_ENABLED
AUDIT_LOG_RETENTION_DAYS
VITE_MAX_UPLOAD_SIZE_MB
Hospital__Name
LINE_ENABLED
LINE_ACCESS_TOKEN
LINE_CHANNEL_SECRET
LINE_RETRY_ENABLED
LINE_RETRY_MAX_ATTEMPTS
LINE_RETRY_INTERVAL_MINUTES
FILE_SCAN_ENABLED
FILE_SCAN_PROVIDER
FILE_SCAN_FAIL_CLOSED
CLAMAV_HOST
CLAMAV_PORT
APPROVAL_ESCALATION_ENABLED
APPROVAL_ESCALATION_INTERVAL_MINUTES
LOGIN_RATE_LIMIT_ENABLED
LOGIN_RATE_LIMIT_MAX_FAILED_ATTEMPTS
LOGIN_RATE_LIMIT_WINDOW_MINUTES
LOGIN_RATE_LIMIT_LOCKOUT_MINUTES
AUTH_TOKEN_STORAGE_MODE
AUTH_COOKIE_SECURE
AUTH_COOKIE_SAMESITE
AUTH_COOKIE_DOMAIN
AUTH_COOKIE_CSRF_ENABLED
VITE_AUTH_TOKEN_STORAGE_MODE
VITE_AUTH_CSRF_COOKIE_NAME
VITE_AUTH_CSRF_HEADER_NAME
```

Run latest migration:

```bash
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

## Database Migration Owner Fix

If EF Core migration fails with:

```text
ERROR: must be owner of table leave_approvals
```

the PostgreSQL application user can read/write the table but does not own it, so PostgreSQL blocks schema changes such as `ALTER TABLE`.

Use these runbooks:

- `docs/DATABASE-OWNER-FIX.md`
- `docs/MIGRATION-RUNBOOK.md`

DBA or PostgreSQL superuser should fix table ownership, then run:

```bash
dotnet tool run dotnet-ef database update --project backend\Hop.Api\Hop.Api.csproj --startup-project backend\Hop.Api\Hop.Api.csproj
```

Do not hardcode database passwords in scripts or documentation. Use a secure prompt, secret manager, or short-lived environment variable.

Upload test:

```powershell
curl.exe -X POST "http://localhost:5000/api/leave-requests/<id>/attachments" `
  -H "Authorization: Bearer <token>" `
  -F "file=@C:\Temp\sample.pdf;type=application/pdf"
```

More details:

- `docs/LEAVE-MODULE.md`
- `docs/LEAVE-PRODUCTION-READINESS.md`
- `docs/LEAVE-PDF.md`
- `docs/LINE-MESSAGING.md`
- `docs/DASHBOARD-METRICS.md`
- `docs/TESTING.md`
- `docs/CI-CD.md`
- `docs/DATABASE-MIGRATION.md`
- `docs/LEAVE-CALENDAR.md`
- `docs/LINE-RETRY-WORKER.md`
- `docs/FILE-SCANNING.md`
- `docs/APPROVAL-DELEGATION.md`
- `docs/LEAVE-REPORTS.md`
- `docs/E2E-API-TESTING.md`
- `docs/SECURITY-HARDENING.md`
- `docs/MONITORING.md`
- `docs/DEPLOYMENT.md`
- `docs/PRODUCTION-CHECKLIST.md`
- `docs/APPROVAL-WORKFLOW.md`
- `docs/STORAGE-UPLOAD.md`
- `docs/SESSION-SECURITY.md`
- `docs/AUDIT-RETENTION.md`
- `docs/LINE-INTEGRATION-PLAN.md`

PDF test:

```powershell
curl.exe -L "http://localhost:5000/api/leave-requests/<leave-request-id>/pdf" `
  -H "Authorization: Bearer <token>" `
  -o leave-request.pdf
```

Build and test:

```powershell
dotnet build backend/Hop.Api/Hop.Api.csproj
dotnet test backend/Hop.Api.Tests/Hop.Api.Tests.csproj
cd frontend
npm ci
npm run build
```

Fresh database smoke test:

See `docs/TESTING.md` for the disposable Docker PostgreSQL smoke test that runs EF Core migrations, starts the backend, checks `/healthz`, verifies critical tables, and logs in with the default development admin.

Production admin bootstrap:

See `docs/TESTING.md` and `docs/DEPLOYMENT.md`. Production must use:

```text
Seed__CreateDefaultAdmin=false
```

after the real production admin is bootstrapped and verified.
