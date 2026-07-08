# Phase 1 Deployment Checklist

Use this checklist before the first pilot department deployment.

## Pre-Deploy

- [ ] Server is Ubuntu with Docker Engine and Docker Compose v2.
- [ ] Repository is checked out to `/opt/hop` or the approved deploy path.
- [ ] `/etc/hop/hop-api.env` exists on the server, permission is `600`, and is not committed to Git.
- [ ] Required environment values are set:
  - `ASPNETCORE_ENVIRONMENT`
  - `Jwt__Key`
  - `PUBLIC_APP_URL`
  - `DB_HOST`
  - `DB_PORT`
  - `POSTGRES_DB` or `DB_NAME`
  - `POSTGRES_USER` or `DB_USER`
  - `POSTGRES_PASSWORD` or `DB_PASSWORD`
  - `Line__Enabled` or `LINE_ENABLED`
- [ ] `DB_PASSWORD`, JWT secret, and LINE token are loaded from secret storage or protected server env.
- [ ] `docker compose --env-file .env.production -f docker-compose.prod.yml config` passes.
- [ ] Backup completed with `scripts/backup/backup-hop.sh`.
- [ ] Restore test has been performed on a non-production database.
- [ ] Restore-test evidence recorded using `docs/qa/RESTORE-TEST-EVIDENCE-TEMPLATE.md`.
- [ ] `RUN_BACKUP_BEFORE_MIGRATION=true` or unset for production deploy.
- [ ] Rollback backup artifacts selected and verified if the release includes schema/storage risk.

## Deploy

Run all:

```bash
chmod +x deploy/*.sh scripts/backup/*.sh
ENV_FILE=/etc/hop/hop-api.env COMPOSE_FILE=docker-compose.prod.yml bash deploy/deploy-all.sh
```

Or run step-by-step:

```bash
bash deploy/00-check-env.sh
bash deploy/01-deploy-db.sh
bash deploy/02-deploy-backend.sh
bash deploy/03-deploy-frontend.sh
bash deploy/04-crosscheck.sh
```

Authenticated smoke test after crosscheck:

```bash
RUN_SMOKE_TEST=true \
SMOKE_USERNAME=<smoke-user> \
SMOKE_PASSWORD=<smoke-password-from-secret-manager> \
SMOKE_EXPECT_FORBIDDEN_URL=/api/users \
bash deploy/04-crosscheck.sh
```

## Crosscheck

- [ ] `/health` returns success.
- [ ] `/health/live` returns success.
- [ ] `/health/ready` returns success.
- [ ] `/api/health` returns success.
- [ ] Homepage loads.
- [ ] `/login` works through SPA fallback.
- [ ] `/leave` works through SPA fallback.
- [ ] PostgreSQL is reachable.
- [ ] Required tables exist.
- [ ] Backend storage path is writable.
- [ ] Frontend build does not contain secret markers.
- [ ] LINE Operations Center shows masked config only.
- [ ] If LINE is enabled, `hasAccessToken` and `hasChannelSecret` are true.

## Smoke Test

- [ ] Admin login works.
- [ ] Staff login works.
- [ ] Staff can create and submit leave.
- [ ] Head sees pending approval.
- [ ] Director sees only current assigned approval.
- [ ] `deploy/05-smoke-e2e.sh` passes for the production smoke user.
- [ ] PDF download works.
- [ ] Attachment download works.
- [ ] Audit log records critical actions.

## Post-Deploy

- [ ] Save deploy log.
- [ ] Record image/container IDs.
- [ ] Record backup artifact names.
- [ ] Monitor backend logs for 30 minutes.
- [ ] Monitor LINE delivery logs if enabled.
- [ ] `hop-backup.timer` is enabled or cron backup is installed.
- [ ] `hop-healthcheck.timer` is enabled or equivalent monitoring is installed.
- [ ] Deploy/backup log retention is enabled.
