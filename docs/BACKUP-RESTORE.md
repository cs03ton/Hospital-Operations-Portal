# Backup and Restore

Phase 1 Pilot ต้องสำรองข้อมูลทั้งฐานข้อมูลและไฟล์ runtime เพราะ HOP เก็บข้อมูลสำคัญสองส่วน:

- PostgreSQL database
- Storage folder ของ backend
- Leave attachments
- Profile images
- PDF templates และ generated files
- Config examples ที่ไม่ใช่ secret เช่น `.env.production.example`, `docker-compose.prod.yml`, `deploy/nginx.conf`

ห้าม commit หรือ backup ค่า secret/token แบบ plain text ลง repository เช่น `.env.production`, JWT secret, LINE token, database password

## Scripts

Linux/Ubuntu scripts:

```text
scripts/backup/backup-hop.sh
scripts/backup/restore-hop.sh
```

On Ubuntu, make scripts executable after deploy:

```bash
chmod +x /opt/hop/scripts/backup/backup-hop.sh /opt/hop/scripts/backup/restore-hop.sh
```

## Required Environment

| Variable | Required | Description |
|---|---|---|
| `DB_HOST` | Yes | PostgreSQL host. For Docker mode usually `localhost` inside container or the container network host |
| `DB_PORT` | Yes | PostgreSQL port, default `5432` |
| `DB_NAME` | Yes | Database name |
| `DB_USER` | Yes | Database user |
| `DB_PASSWORD` | Yes | Database password, pass through environment only |
| `BACKUP_ROOT` | No | Backup output root, default `/opt/hop/backups` |
| `STORAGE_PATH` | No | Host storage folder, production usually `/opt/hop/uploads` |
| `BACKUP_RETENTION_DAYS` | No | Retention in days, default `7` |
| `BACKUP_MODE` | No | `host` or `docker`, default `host` |
| `POSTGRES_CONTAINER` | Docker | PostgreSQL container name, default `hop-prod-postgres` |
| `STORAGE_DOCKER_VOLUME` | Docker | Storage Docker volume name, default `hop_prod_storage` |

Restore additionally uses:

| Variable | Required | Description |
|---|---|---|
| `DB_DUMP_PATH` | No | Path to `.backup` file. If omitted, `restore-hop.sh` uses the latest file in `BACKUP_ROOT/postgres` |
| `STORAGE_ARCHIVE_PATH` | No | Path to storage `.tar.gz` file |
| `RESTORE_CONFIRMATION` | Required with `--yes` | Must be `RESTORE_HOP_DATABASE` |

## Backup Output

The script creates:

```text
backups/
├── postgres/
│   └── hopdb_YYYYMMDD_HHMMSS.backup
├── storage/
│   └── hop_uploads_YYYYMMDD_HHMMSS.tar.gz
├── logs/
│   └── backup_YYYYMMDD_HHMMSS.log
└── YYYYMMDD_HHMMSS/
    ├── hopdb_YYYYMMDD_HHMMSS.backup
    └── hop_uploads_YYYYMMDD_HHMMSS.tar.gz
```

`backups/` is ignored by Git.

## Manual Backup: Host PostgreSQL

Use this when PostgreSQL is installed on the host or reachable over the network:

```bash
export DB_HOST=127.0.0.1
export DB_PORT=5432
export DB_NAME=hop_db
export DB_USER=hop_user
export DB_PASSWORD='set-this-from-secret-manager'
export BACKUP_ROOT=/opt/hop/backups
export STORAGE_PATH=/opt/hop/uploads
export BACKUP_MODE=host

/opt/hop/scripts/backup/backup-hop.sh
```

The script uses `pg_dump --format=custom --no-owner`.

## Manual Backup: Docker PostgreSQL

Use this for the Phase 1 Docker Compose stack:

```bash
export DB_HOST=localhost
export DB_PORT=5432
export DB_NAME="${POSTGRES_DB}"
export DB_USER="${POSTGRES_USER}"
export DB_PASSWORD="${POSTGRES_PASSWORD}"
export BACKUP_ROOT=/opt/hop/backups
export BACKUP_MODE=docker
export POSTGRES_CONTAINER=hop-prod-postgres
export STORAGE_DOCKER_VOLUME=hop_prod_storage

/opt/hop/scripts/backup/backup-hop.sh
```

Manual equivalent:

```bash
docker exec -e PGPASSWORD="$DB_PASSWORD" hop-prod-postgres \
  pg_dump -h localhost -p 5432 -U "$DB_USER" -d "$DB_NAME" -Fc --no-owner \
  > /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup

docker run --rm \
  -v hop_prod_storage:/data:ro \
  -v /opt/hop/backups/storage:/backup \
  alpine:3.20 \
  tar -czf /backup/hop_uploads_YYYYMMDD_HHMMSS.tar.gz -C /data .
```

## Restore Warning

Restore can overwrite production data. Before restore:

1. Confirm maintenance window.
2. Stop user traffic.
3. Confirm selected database dump and storage archive.
4. Confirm rollback plan.
5. For non-interactive restore, export `RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE` and pass `--yes`.

Never run restore against production from an unverified backup.

## Restore: Host PostgreSQL

```bash
export DB_HOST=127.0.0.1
export DB_PORT=5432
export DB_NAME=hop_db
export DB_USER=hop_user
export DB_PASSWORD='set-this-from-secret-manager'
export BACKUP_MODE=host
export STORAGE_PATH=/opt/hop/uploads
export DB_DUMP_PATH=/opt/hop/backups/postgres/hopdb_20260630_020000.backup
export STORAGE_ARCHIVE_PATH=/opt/hop/backups/storage/hop_uploads_20260630_020000.tar.gz
export RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE

/opt/hop/scripts/backup/restore-hop.sh
```

The script uses:

```bash
pg_restore --clean --if-exists --no-owner
```

## Restore: Docker PostgreSQL

```bash
export DB_HOST=localhost
export DB_PORT=5432
export DB_NAME="${POSTGRES_DB}"
export DB_USER="${POSTGRES_USER}"
export DB_PASSWORD="${POSTGRES_PASSWORD}"
export BACKUP_MODE=docker
export POSTGRES_CONTAINER=hop-prod-postgres
export STORAGE_DOCKER_VOLUME=hop_prod_storage
export DB_DUMP_PATH=/opt/hop/backups/postgres/hopdb_20260630_020000.backup
export STORAGE_ARCHIVE_PATH=/opt/hop/backups/storage/hop_uploads_20260630_020000.tar.gz
export RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE

/opt/hop/scripts/backup/restore-hop.sh
```

Manual equivalent:

```bash
cat /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup | \
  docker exec -i -e PGPASSWORD="$DB_PASSWORD" hop-prod-postgres \
  pg_restore -h localhost -p 5432 -U "$DB_USER" -d "$DB_NAME" \
  --clean --if-exists --no-owner

docker run --rm \
  -v hop_prod_storage:/data \
  -v /opt/hop/backups/storage:/backup:ro \
  alpine:3.20 \
  sh -c 'rm -rf /data/* /data/.[!.]* /data/..?* 2>/dev/null || true; tar -xzf /backup/hop_uploads_YYYYMMDD_HHMMSS.tar.gz -C /data'
```

## Cron Example

Run one combined backup every day at 02:00:

```cron
0 2 * * * /opt/hop/scripts/backup/backup-hop.sh >> /var/log/hop-backup.log 2>&1
```

Recommended `/etc/hop/backup.env`:

```bash
DB_HOST=localhost
DB_PORT=5432
DB_NAME=hop_db
DB_USER=hop_user
BACKUP_ROOT=/opt/hop/backups
BACKUP_MODE=docker
POSTGRES_CONTAINER=hop-prod-postgres
STORAGE_DOCKER_VOLUME=hop_prod_storage
BACKUP_RETENTION_DAYS=30
```

For bare-metal runtime, use:

```text
STORAGE_PATH=/opt/hop/uploads
```

Load secret separately in cron instead of committing it:

```cron
0 2 * * * . /etc/hop/backup.env; export DB_PASSWORD="$(cat /run/secrets/hop_db_password)"; /opt/hop/scripts/backup/backup-hop.sh >> /var/log/hop-backup.log 2>&1
```

## Systemd Timer Example

Recommended for production Ubuntu servers:

```bash
sudo mkdir -p /etc/hop /var/log/hop
sudo cp /opt/hop/systemd/hop-backup.service.example /etc/systemd/system/hop-backup.service
sudo cp /opt/hop/systemd/hop-backup.timer.example /etc/systemd/system/hop-backup.timer
sudo systemctl daemon-reload
sudo systemctl enable --now hop-backup.timer
sudo systemctl list-timers hop-backup.timer
```

Example `/etc/hop/backup.env`:

```text
BACKUP_MODE=docker
BACKUP_ROOT=/opt/hop/backups
BACKUP_RETENTION_DAYS=30
DB_HOST=localhost
DB_PORT=5432
DB_NAME=hop_db
DB_USER=hop_user
POSTGRES_CONTAINER=hop-prod-postgres
STORAGE_DOCKER_VOLUME=hop_prod_storage
```

Load `DB_PASSWORD` from a protected systemd credential, secret manager, or environment override. Do not put real passwords in repository files.

## Mandatory Backup Before Migration

Production database migration path uses `deploy/01-deploy-db.sh`. This script runs `scripts/backup/backup-hop.sh` before EF Core migrations by default:

```bash
ENV_FILE=.env.production COMPOSE_FILE=docker-compose.prod.yml bash deploy/01-deploy-db.sh
```

If the backup fails, migration is stopped and the deploy must not continue.

Emergency skip requires explicit approval:

```bash
RUN_BACKUP_BEFORE_MIGRATION=false \
SKIP_BACKUP_CONFIRM=I_ACCEPT_MIGRATION_WITHOUT_BACKUP \
bash deploy/01-deploy-db.sh
```

## Restore Evidence

Use `docs/qa/RESTORE-TEST-EVIDENCE-TEMPLATE.md` for monthly restore drills.

Minimum evidence to keep:

- Backup timestamp and artifact names
- Restore target environment
- Restore log path
- Health check result for `/health/live` and `/health/ready`
- Login result
- Leave request, attachment, PDF, and audit log verification result

## Retention

Default:

```text
BACKUP_RETENTION_DAYS=30
```

Minimum recommendation:

| Backup | Retention |
|---|---|
| Daily | 14-30 days |
| Weekly | 8-12 weeks |
| Monthly | 12 months, stored off-server |

The script currently enforces day-based retention. Weekly/monthly archival should be handled by server policy or external storage lifecycle rules.

## Verify Backup

After every backup job:

1. Check non-zero file sizes:
   ```bash
   ls -lh /opt/hop/backups/postgres/*.backup /opt/hop/backups/storage/*.tar.gz
   ```
2. Check latest log:
   ```bash
   tail -100 /opt/hop/backups/logs/backup_*.log
   ```
3. Verify dump metadata:
   ```bash
   pg_restore --list /opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup | head
   ```
4. Verify storage archive:
   ```bash
   tar -tzf /opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz | head
   ```
5. Login as Admin/SuperAdmin and open `/admin/health`.
6. Confirm Backup Status shows the latest backup timestamp and file metadata from `BACKUP_ROOT`.

## Monthly Restore Test Checklist

Run monthly against a test server or disposable database only:

1. Create a test DB.
2. Restore the latest `.backup`.
3. Restore the latest storage `.tar.gz`.
4. Start backend/frontend against the restored DB.
5. Verify `/healthz`.
6. Login with a test admin account.
7. Verify users and roles.
8. Verify leave requests and approval timeline.
9. Verify leave balance data.
10. Verify profile images.
11. Verify leave attachments download.
12. Verify PDF template and PDF generation.
13. Verify audit logs.
14. Create restore-test evidence in the backup evidence folder or naming convention that includes `restore`.
15. Open `/admin/health` and confirm Last Restore Test is visible when evidence exists.
16. Record result in deployment checklist.

## Health and Alerting

The backup script:

- Writes timestamped logs.
- Returns a non-zero exit code on failure.
- Is safe to wire into cron, systemd timer, or monitoring.

Future hook:

- Add LINE notification or monitoring webhook when backup fails.
- Do not put LINE tokens in the script or repository; load them from environment or secret manager.
- Health Center reads backup metadata only; it does not execute backup or restore commands.

## Production Checklist

- [ ] `BACKUP_ROOT` points to a disk with enough free space.
- [ ] Backup directory is outside the Git repository or ignored by Git.
- [ ] DB password is loaded from environment or secret manager.
- [ ] Storage volume/path is included.
- [ ] Restore script tested on a disposable environment.
- [ ] Monthly restore test owner assigned.
- [ ] Off-server copy configured.
- [ ] Backup failure alert planned or configured.
- [ ] Production restore requires explicit approval and maintenance window.
