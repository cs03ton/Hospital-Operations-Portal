#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

source scripts/deploy/load-hop-env.sh
load_hop_env

HOP_ROOT="${HOP_ROOT:-/opt/hop}"
UPLOADS_PATH="${UPLOADS_PATH:-${HOP_ROOT}/uploads}"
BACKUP_ROOT="${BACKUP_ROOT:-${HOP_ROOT}/backups}"
RELEASE_ID="${RELEASE_ID:-$(date +%Y%m%d_%H%M%S)}"
RUN_BACKUP_BEFORE_MIGRATION="${RUN_BACKUP_BEFORE_MIGRATION:-true}"
SKIP_BACKUP_CONFIRM="${SKIP_BACKUP_CONFIRM:-}"
SKIP_BACKUP_CONFIRM_TEXT="I_ACCEPT_MIGRATION_WITHOUT_BACKUP"
RUN_MIGRATIONS="${RUN_MIGRATIONS:-true}"
HOP_API_SERVICE="${HOP_API_SERVICE:-hop-api}"
NGINX_SERVICE="${NGINX_SERVICE:-nginx}"
FRONTEND_URL="${FRONTEND_URL:-${PUBLIC_APP_URL:-http://localhost}}"

export RELEASE_ID
export STORAGE_PATH="${STORAGE_PATH:-$UPLOADS_PATH}"
export Storage__RootPath="${Storage__RootPath:-$UPLOADS_PATH}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

run_backup() {
  if [ "$RUN_BACKUP_BEFORE_MIGRATION" != "true" ]; then
    [ "$SKIP_BACKUP_CONFIRM" = "$SKIP_BACKUP_CONFIRM_TEXT" ] || fail "Backup before migration is mandatory. Set SKIP_BACKUP_CONFIRM=${SKIP_BACKUP_CONFIRM_TEXT} only for approved emergency skip."
    log "WARNING: backup before migration explicitly skipped"
    return
  fi

  log "Running mandatory backup before migration"
  BACKUP_MODE=host \
  BACKUP_ROOT="$BACKUP_ROOT" \
  STORAGE_PATH="$UPLOADS_PATH" \
  DB_HOST="${DB_HOST:-127.0.0.1}" \
  DB_PORT="${DB_PORT:-5432}" \
  DB_NAME="$DB_NAME" \
  DB_USER="$DB_USER" \
  DB_PASSWORD="$DB_PASSWORD" \
    bash scripts/backup/backup-hop.sh
}

run_migrations() {
  [ "$RUN_MIGRATIONS" = "true" ] || {
    log "RUN_MIGRATIONS=false; skipping EF Core migrations"
    return
  }

  command -v dotnet >/dev/null 2>&1 || fail "dotnet SDK is required for EF Core migrations."
  log "Running EF Core migrations"
  dotnet tool restore >/dev/null
  dotnet tool run dotnet-ef database update \
    --project backend/Hop.Api/Hop.Api.csproj \
    --startup-project backend/Hop.Api/Hop.Api.csproj >/dev/null
}

restart_services() {
  log "Restarting ${HOP_API_SERVICE}"
  sudo systemctl restart "$HOP_API_SERVICE"
  sudo systemctl status "$HOP_API_SERVICE" --no-pager --lines=20

  log "Reloading ${NGINX_SERVICE}"
  sudo nginx -t
  sudo systemctl reload "$NGINX_SERVICE"
}

crosscheck() {
  log "Running bare-metal crosscheck"
  curl -fsS "${FRONTEND_URL}/" >/dev/null
  curl -fsS "${FRONTEND_URL}/health/live" >/dev/null
  curl -fsS "${FRONTEND_URL}/health/ready" >/dev/null
}

log "Starting HOP bare-metal deploy release ${RELEASE_ID}"
mkdir -p "${HOP_ROOT}/backend" "$UPLOADS_PATH" "${HOP_ROOT}/logs" "$BACKUP_ROOT" "${HOP_ROOT}/releases"

run_backup
run_migrations
bash scripts/deploy/publish-backend-baremetal.sh
bash scripts/deploy/publish-frontend-baremetal.sh
restart_services
crosscheck

log "HOP bare-metal deploy completed: ${RELEASE_ID}"
