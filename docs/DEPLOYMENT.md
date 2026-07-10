# Deployment

## Production Environment Files

Production ต้องใช้ไฟล์ `/etc/hop/hop-api.env`, `.env.production` หรือ Secret Manager ของเครื่องแม่ข่ายเท่านั้น

ค่าเริ่มต้นของ deploy scripts:

1. ถ้ามี `/etc/hop/hop-api.env` จะใช้ไฟล์นี้อัตโนมัติ
2. ถ้าไม่มี จะ fallback ไปที่ `.env.production` สำหรับ local/staging

```bash
sudo mkdir -p /etc/hop
sudo cp .env.production.example /etc/hop/hop-api.env
sudo nano /etc/hop/hop-api.env
./deploy/00-check-env.sh
```

ห้ามใส่ค่าลับลงใน `backend/Hop.Api/appsettings.json` หรือ `backend/Hop.Api/appsettings.Development.json`

ค่าที่ต้องตั้งอย่างน้อย:

| Key | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Key` | JWT signing key |
| `Line__AccessToken` | LINE Messaging API access token |
| `Line__ChannelSecret` | LINE webhook signature verification |
| `Storage__RootPath` | Runtime storage folder |
| `Storage__PublicBaseUrl` | Public base URL for profile images/files |

## Production Startup

```bash
docker compose --env-file .env.production -f docker-compose.prod.yml pull
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
docker compose --env-file .env.production -f docker-compose.prod.yml ps
```

## Update Deployment

```bash
git pull
./deploy/00-check-env.sh
RUN_SMOKE_TEST=true \
SMOKE_USERNAME=<smoke-user> \
SMOKE_PASSWORD=<smoke-password-from-secret-manager> \
bash deploy/deploy-all.sh
```

`deploy/01-deploy-db.sh` runs `scripts/backup/backup-hop.sh` before EF Core migrations by default. If the backup fails, migrations stop.

Emergency skip requires explicit approval:

```bash
RUN_BACKUP_BEFORE_MIGRATION=false \
SKIP_BACKUP_CONFIRM=I_ACCEPT_MIGRATION_WITHOUT_BACKUP \
bash deploy/01-deploy-db.sh
```

Do not use this skip path for normal production deployment.

## Rollback

Application-only rollback:

```bash
ROLLBACK_REF=<previous-release-tag> \
ROLLBACK_CONFIRM=I_UNDERSTAND_ROLLBACK_WILL_REDEPLOY_APP \
bash deploy/rollback.sh
```

Application + database + storage rollback from a verified backup:

```bash
ROLLBACK_REF=<previous-release-tag> \
ROLLBACK_CONFIRM=I_UNDERSTAND_ROLLBACK_WILL_REDEPLOY_APP \
RESTORE_CONFIRM=I_UNDERSTAND_THIS_WILL_OVERWRITE_HOP \
RESTORE_DATABASE=true \
RESTORE_STORAGE=true \
DB_DUMP_PATH=/opt/hop/backups/db/hop_db_YYYYMMDD_HHMMSS.dump \
STORAGE_ARCHIVE_PATH=/opt/hop/backups/storage/hop_storage_YYYYMMDD_HHMMSS.tar.gz \
bash deploy/rollback.sh
```

> Warning: Database/storage rollback must be approved, performed in a maintenance window, and use a backup that passed restore-test evidence.

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

Use `docs/PHASE1-PILOT-CHECKLIST.md` for the first department pilot.

## Production Docker Compose

Phase 1 production uses:

```text
docker-compose.prod.yml
.env.production
deploy/nginx.conf
```

Create the production environment file:

```powershell
Copy-Item .env.production.example .env.production
notepad .env.production
```

On Ubuntu production server:

```bash
sudo mkdir -p /etc/hop
sudo cp .env.production.example /etc/hop/hop-api.env
sudo nano /etc/hop/hop-api.env
sudo chmod 600 /etc/hop/hop-api.env
```

Validate compose configuration:

```powershell
docker compose --env-file .env.production -f docker-compose.prod.yml config
```

or on Ubuntu production:

```bash
ENV_FILE=/etc/hop/hop-api.env HOP_API_ENV_FILE=/etc/hop/hop-api.env \
docker compose --env-file /etc/hop/hop-api.env -f docker-compose.prod.yml config
```

Start the stack:

```powershell
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
```

Check service status:

```powershell
docker compose --env-file .env.production -f docker-compose.prod.yml ps
```

Verify health:

```powershell
curl http://localhost/healthz
curl http://localhost/health/live
curl http://localhost/health/ready
```

Admin/SuperAdmin must also verify the Phase 1.5 Health Center after login:

1. Open `/admin/health`.
2. Confirm Overall Status is not `Unhealthy`.
3. Confirm API, Database, Storage, LINE, Disk, CPU, RAM, Backup, Version, Environment, and Server Time are visible.
4. Confirm the network response does not contain secrets, tokens, passwords, or connection strings.
5. If Backup is `Warning` or `Unhealthy`, run a manual backup and refresh the page.

## Phase 1 Deployment Scripts

Ubuntu deployment scripts live in `deploy/`:

```text
deploy/00-check-env.sh
deploy/01-deploy-db.sh
deploy/02-deploy-backend.sh
deploy/03-deploy-frontend.sh
deploy/04-crosscheck.sh
deploy/deploy-all.sh
deploy/rollback.sh
```

Make scripts executable on the server:

```bash
chmod +x deploy/*.sh scripts/backup/*.sh
```

Run the full sequence:

```bash
ENV_FILE=.env.production COMPOSE_FILE=docker-compose.prod.yml bash deploy/deploy-all.sh
```

Run step-by-step:

```bash
bash deploy/00-check-env.sh
bash deploy/01-deploy-db.sh
bash deploy/02-deploy-backend.sh
bash deploy/03-deploy-frontend.sh
bash deploy/04-crosscheck.sh
```

`01-deploy-db.sh` starts PostgreSQL, waits for readiness, runs EF Core migrations, and checks critical tables. If the backend container uses `DB_HOST=postgres`, set host migration connection explicitly when needed:

```bash
MIGRATION_DB_HOST=127.0.0.1 MIGRATION_DB_PORT=5432 deploy/01-deploy-db.sh
```

Before deploying, run backup:

```bash
BACKUP_MODE=docker BACKUP_ROOT=/opt/hop/backups scripts/backup/backup-hop.sh
```

See `docs/DEPLOYMENT-CHECKLIST.md` for the release checklist and `docs/ROLLBACK.md` for rollback commands.

## Production Database Migration

Run EF Core migrations before opening traffic to users:

```powershell
$env:ConnectionStrings__DefaultConnection='Host=<db-host>;Port=5432;Database=hop_db;Username=hop_app;Password=<password>'
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend/Hop.Api/Hop.Api.csproj --startup-project backend/Hop.Api/Hop.Api.csproj
```

When using Docker-only deployment, run migrations from a trusted admin workstation or CI runner that can reach PostgreSQL. Do not enable uncontrolled production startup seeding.

## Admin Bootstrap

Production default:

```text
Database__SeedOnStartup=false
Seed__CreateDefaultAdmin=false
```

Temporary bootstrap is allowed only before go-live:

```text
Database__SeedOnStartup=true
Seed__CreateDefaultAdmin=true
Seed__AdminUsername=<admin-username>
Seed__AdminPassword=<strong-temporary-password>
Seed__AdminFullName=<admin-full-name>
Seed__AdminEmployeeCode=<employee-code>
```

After first login:

1. Change the password.
2. Create named admin/support users.
3. Set `Seed__CreateDefaultAdmin=false`.
4. Set `Database__SeedOnStartup=false`.
5. Restart backend.
6. Confirm the named production admin can still login.

## Update Command

Before updating:

```powershell
.\scripts\backup-postgres.ps1 -EnvFile .env.production -ComposeFile docker-compose.prod.yml
```

Deploy updated images:

```powershell
docker compose --env-file .env.production -f docker-compose.prod.yml pull
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
docker compose --env-file .env.production -f docker-compose.prod.yml ps
```

## Rollback Command

Rollback must restore both database and application version.

```powershell
git checkout <previous-release-tag>
docker compose --env-file .env.production -f docker-compose.prod.yml up -d --build
.\scripts\restore-postgres.ps1 -BackupFile database/backup/<backup-file>.dump -EnvFile .env.production -ComposeFile docker-compose.prod.yml
docker compose --env-file .env.production -f docker-compose.prod.yml restart backend frontend nginx
```

After rollback, verify:

- `/healthz`
- admin login
- leave request list
- attachment download
- PDF download

## Backup and Restore

Detailed commands are documented in `docs/BACKUP-RESTORE.md`.

Minimum production routine:

```powershell
.\scripts\backup-postgres.ps1 -EnvFile .env.production -ComposeFile docker-compose.prod.yml
```

Keep PostgreSQL dumps and uploaded-file backups together for each release. Uploaded-file storage includes leave attachments and `storage/profile-images`.

For LINE Flex avatar images, configure a public file URL reachable by LINE:

```text
PUBLIC_FILE_BASE_URL=https://your-hop-public-domain
```

If this is not configured, HOP falls back to initials avatar in LINE Flex messages.

## Production Notes

- Do not use the default development admin in production.
- Do not use production database for tests.
- Store secrets in the deployment secret manager, not `.env` committed files.
- Back up PostgreSQL and uploaded file storage before migrations.
- Enable ClamAV fail-closed mode for production uploads.
- Use `ASPNETCORE_ENVIRONMENT=Production`.
- Use an application database user that is not a PostgreSQL superuser.
- Restrict CORS origins to the production frontend URL.
