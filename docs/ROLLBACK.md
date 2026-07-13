# Rollback

Rollback must be deliberate. Application rollback and database rollback are separate operations.

## Application Rollback

Use when the new backend/frontend deploy fails but database data is still valid.

```bash
export ROLLBACK_REF=<previous-release-tag-or-commit>
export ROLLBACK_CONFIRM=I_UNDERSTAND_ROLLBACK_WILL_REDEPLOY_APP
ENV_FILE=.env.production COMPOSE_FILE=docker-compose.prod.yml bash deploy/rollback.sh
```

If `ROLLBACK_REF` is omitted, the script redeploys the current checkout.

The rollback script:

- Optionally checks out `ROLLBACK_REF`.
- Rebuilds backend and frontend containers.
- Restarts backend, frontend, and nginx.
- Runs `deploy/04-crosscheck.sh`.

## Database Rollback

Do not automatically rollback the database unless the migration is confirmed destructive and a maintenance window is approved.

Use the backup/restore runbook:

```bash
export BACKUP_MODE=docker
export DB_NAME="${POSTGRES_DB}"
export DB_USER="${POSTGRES_USER}"
export DB_PASSWORD="${POSTGRES_PASSWORD}"
export DB_DUMP_PATH=/opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup
export STORAGE_ARCHIVE_PATH=/opt/hop/backups/storage/hop_uploads_YYYYMMDD_HHMMSS.tar.gz
export RESTORE_CONFIRMATION=RESTORE_HOP_DATABASE
scripts/backup/restore-hop.sh
```

## Rollback Checklist

- [ ] Confirm incident owner.
- [ ] Confirm affected version.
- [ ] Confirm backup artifact.
- [ ] Stop user traffic if database restore is needed.
- [ ] Run application rollback.
- [ ] Run crosscheck.
- [ ] Run login and leave smoke tests.
- [ ] If database restore was done, verify attachments, profile images, PDF templates, and audit logs.
- [ ] Write incident note with cause and next action.
