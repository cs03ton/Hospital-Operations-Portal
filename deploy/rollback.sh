#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

DEFAULT_ENV_FILE="/etc/hop/hop-api.env"
if [ -z "${ENV_FILE:-}" ]; then
  if [ -f "$DEFAULT_ENV_FILE" ]; then
    ENV_FILE="$DEFAULT_ENV_FILE"
  else
    ENV_FILE=".env.production"
  fi
fi
export HOP_API_ENV_FILE="${HOP_API_ENV_FILE:-$ENV_FILE}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
ROLLBACK_REF="${ROLLBACK_REF:-}"
ROLLBACK_CONFIRM="${ROLLBACK_CONFIRM:-}"
CONFIRM_TEXT="I_UNDERSTAND_ROLLBACK_WILL_REDEPLOY_APP"
RESTORE_DATABASE="${RESTORE_DATABASE:-false}"
RESTORE_STORAGE="${RESTORE_STORAGE:-false}"
RESTORE_SCRIPT="${RESTORE_SCRIPT:-scripts/backup/restore-hop.sh}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

[ -f "$ENV_FILE" ] || fail "$ENV_FILE not found."

if [ "$ROLLBACK_CONFIRM" != "$CONFIRM_TEXT" ]; then
  cat <<EOF
WARNING: This rollback can redeploy application containers and optionally restore
database/storage from a verified backup.

Set this environment variable to continue:
  ROLLBACK_CONFIRM=${CONFIRM_TEXT}

Optional restore flags:
  RESTORE_DATABASE=true DB_DUMP_PATH=/opt/hop/backups/postgres/hopdb_YYYYMMDD_HHMMSS.backup
  RESTORE_STORAGE=true STORAGE_ARCHIVE_PATH=/opt/hop/backups/storage/<storage>.tar.gz
  RESTORE_CONFIRM=I_UNDERSTAND_THIS_WILL_OVERWRITE_HOP
EOF
  exit 2
fi

restore_data_if_requested() {
  if [ "$RESTORE_DATABASE" != "true" ] && [ "$RESTORE_STORAGE" != "true" ]; then
    log "Database/storage restore not requested; running app rollback only"
    return
  fi

  [ -f "$RESTORE_SCRIPT" ] || fail "Restore script not found: $RESTORE_SCRIPT"
  [ "${RESTORE_CONFIRM:-}" = "I_UNDERSTAND_THIS_WILL_OVERWRITE_HOP" ] || fail "RESTORE_CONFIRM is required for database/storage restore."

  if [ "$RESTORE_DATABASE" = "true" ] && [ -z "${DB_DUMP_PATH:-}" ]; then
    fail "DB_DUMP_PATH is required when RESTORE_DATABASE=true"
  fi

  if [ "$RESTORE_STORAGE" = "true" ] && [ -z "${STORAGE_ARCHIVE_PATH:-}" ]; then
    fail "STORAGE_ARCHIVE_PATH is required when RESTORE_STORAGE=true"
  fi

  log "Stopping app services before database/storage restore"
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" stop backend frontend nginx >/dev/null || true

  log "Restoring selected data from backup"
  bash "$RESTORE_SCRIPT"
  log "Database/storage restore completed"
}

if [ -n "$ROLLBACK_REF" ]; then
  log "Checking out rollback ref: ${ROLLBACK_REF}"
  git checkout "$ROLLBACK_REF"
else
  log "ROLLBACK_REF not provided; redeploying current checkout"
fi

restore_data_if_requested

log "Rebuilding and restarting application services"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" build backend frontend
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d backend frontend nginx

log "Running crosscheck after rollback"
bash deploy/04-crosscheck.sh
log "Rollback completed"
