# Backup and Restore

Phase 1 PostgreSQL backups use `pg_dump` custom format. Uploaded files are stored in a Docker volume and must be backed up separately.

## Backup Command

Using the production compose stack:

```powershell
.\scripts\backup-postgres.ps1 -EnvFile .env.production -ComposeFile docker-compose.prod.yml
```

The script writes a `.dump` file to:

```text
database/backup/
```

Manual equivalent:

```powershell
docker compose --env-file .env.production -f docker-compose.prod.yml exec -T postgres sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc --file "/backup/hop_db_YYYYMMDD_HHMMSS.dump"'
```

## Restore Command

```powershell
.\scripts\restore-postgres.ps1 -BackupFile database/backup/<backup-file>.dump -EnvFile .env.production -ComposeFile docker-compose.prod.yml
```

Manual equivalent:

```powershell
docker compose --env-file .env.production -f docker-compose.prod.yml up -d postgres
docker compose --env-file .env.production -f docker-compose.prod.yml exec -T postgres sh -c 'pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists "/backup/<backup-file>.dump"'
```

## Uploaded File Storage

Production uploads are stored in the `hop_prod_storage` Docker volume at `/app/storage` inside the backend container. This includes leave attachments and profile images under `profile-images/`.

Back up uploaded files before migrations and before application updates.

Suggested server-side flow:

```powershell
docker run --rm -v hop_prod_storage:/data -v ${PWD}/database/backup:/backup alpine tar czf /backup/hop_storage_YYYYMMDD_HHMMSS.tgz -C /data .
```

## Restore Checklist

1. Stop backend and frontend traffic.
2. Restore PostgreSQL from the selected dump.
3. Restore uploaded file storage if needed.
4. Start the stack.
5. Verify `/healthz`.
6. Login with a production admin account.
7. Verify leave requests, attachments, PDF download, and audit log.

## Retention

Recommended minimum:

| Backup | Retention |
|---|---|
| Daily | 30 days |
| Weekly | 12 weeks |
| Monthly | 12 months |
