# Backup Strategy

Phase 1.2 defines the production backup and restore foundation.

## Database Backup

Daily backup:

```powershell
$env:PGPASSWORD="<postgres-password>"
pg_dump -h localhost -U postgres -d hop_db -Fc -f database/backup/hop_db_$(Get-Date -Format yyyyMMdd_HHmmss).dump
```

Weekly full backup should use the same command and store the output in a weekly folder.

## Retention

```text
Daily:   30 days
Weekly:  12 weeks
Monthly: 12 months
```

## Database Restore

Restore into an existing database:

```powershell
$env:PGPASSWORD="<postgres-password>"
pg_restore -h localhost -U postgres -d hop_db --clean --if-exists database/backup/<backup-file>.dump
```

Restore into a new database:

```powershell
$env:PGPASSWORD="<postgres-password>"
createdb -h localhost -U postgres hop_db_restore
pg_restore -h localhost -U postgres -d hop_db_restore database/backup/<backup-file>.dump
```

## Docker Restore

1. Stop services.
2. Restore PostgreSQL data from dump.
3. Start services.
4. Verify `/healthz` and login with an admin account.

```powershell
docker compose down
docker compose up -d postgres
pg_restore -h localhost -U postgres -d hop_db --clean --if-exists database/backup/<backup-file>.dump
docker compose up -d
```

## Uploaded Files

Uploaded files should be stored outside containers in a mounted volume.

Back up the upload volume daily and restore it before starting application services.
