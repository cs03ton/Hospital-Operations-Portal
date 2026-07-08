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
POSTGRES_SERVICE="${POSTGRES_SERVICE:-postgres}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-hop-prod-postgres}"
MIGRATION_TIMEOUT_SECONDS="${MIGRATION_TIMEOUT_SECONDS:-120}"
MIGRATION_DB_HOST="${MIGRATION_DB_HOST:-127.0.0.1}"
MIGRATION_DB_PORT="${MIGRATION_DB_PORT:-${POSTGRES_PORT:-5432}}"
RUN_BACKUP_BEFORE_MIGRATION="${RUN_BACKUP_BEFORE_MIGRATION:-true}"
BACKUP_SCRIPT="${BACKUP_SCRIPT:-scripts/backup/backup-hop.sh}"
BACKUP_ROOT="${BACKUP_ROOT:-./backups}"
BACKUP_MODE="${BACKUP_MODE:-docker}"
STORAGE_DOCKER_VOLUME="${STORAGE_DOCKER_VOLUME:-hop_prod_storage}"
SKIP_BACKUP_CONFIRM="${SKIP_BACKUP_CONFIRM:-}"
SKIP_BACKUP_CONFIRM_TEXT="I_ACCEPT_MIGRATION_WITHOUT_BACKUP"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

load_env() {
  [ -f "$ENV_FILE" ] || fail "$ENV_FILE not found."
  while IFS= read -r line || [ -n "$line" ]; do
    line="${line%$'\r'}"
    case "$line" in
      ''|\#*) continue ;;
    esac
    key="${line%%=*}"
    value="${line#*=}"
    if [[ "$key" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]]; then
      if [[ "$value" == \"*\" && "$value" == *\" ]]; then
        value="${value:1:${#value}-2}"
      elif [[ "$value" == \'*\' && "$value" == *\' ]]; then
        value="${value:1:${#value}-2}"
      fi
      export "$key=$value"
    fi
  done < "$ENV_FILE"
  export DB_NAME="${DB_NAME:-${POSTGRES_DB:-}}"
  export DB_USER="${DB_USER:-${POSTGRES_USER:-}}"
  export DB_PASSWORD="${DB_PASSWORD:-${POSTGRES_PASSWORD:-}}"
  export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=${MIGRATION_DB_HOST};Port=${MIGRATION_DB_PORT};Database=${DB_NAME:-};Username=${DB_USER:-};Password=${DB_PASSWORD:-}}"
}

wait_for_postgres() {
  log "Waiting for PostgreSQL readiness"
  local end=$((SECONDS + MIGRATION_TIMEOUT_SECONDS))
  until docker exec -e PGPASSWORD="$DB_PASSWORD" "$POSTGRES_CONTAINER" pg_isready -U "$DB_USER" -d "$DB_NAME" >/dev/null 2>&1; do
    if [ "$SECONDS" -ge "$end" ]; then
      fail "PostgreSQL did not become ready within ${MIGRATION_TIMEOUT_SECONDS}s"
    fi
    sleep 2
  done
  log "PostgreSQL is ready"
}

backup_before_migration() {
  if [ "$RUN_BACKUP_BEFORE_MIGRATION" != "true" ]; then
    if [ "$SKIP_BACKUP_CONFIRM" != "$SKIP_BACKUP_CONFIRM_TEXT" ]; then
      cat <<EOF
ERROR: Backup before migration is mandatory for production safety.

To skip only in an approved emergency, set:
  RUN_BACKUP_BEFORE_MIGRATION=false
  SKIP_BACKUP_CONFIRM=${SKIP_BACKUP_CONFIRM_TEXT}
EOF
      exit 2
    fi

    log "WARNING: backup before migration was explicitly skipped"
    return
  fi

  [ -f "$BACKUP_SCRIPT" ] || fail "Backup script not found: $BACKUP_SCRIPT"
  log "Running mandatory backup before EF Core migration"

  BACKUP_MODE="$BACKUP_MODE" \
  DB_HOST="${BACKUP_DB_HOST:-localhost}" \
  DB_PORT="${BACKUP_DB_PORT:-5432}" \
  DB_NAME="$DB_NAME" \
  DB_USER="$DB_USER" \
  DB_PASSWORD="$DB_PASSWORD" \
  BACKUP_ROOT="$BACKUP_ROOT" \
  POSTGRES_CONTAINER="$POSTGRES_CONTAINER" \
  STORAGE_PATH="${STORAGE_PATH:-./storage}" \
  STORAGE_DOCKER_VOLUME="$STORAGE_DOCKER_VOLUME" \
    bash "$BACKUP_SCRIPT"

  log "Mandatory backup completed before migration"
}

run_migrations() {
  command -v dotnet >/dev/null 2>&1 || fail "dotnet SDK is required to run EF Core migrations from host."
  log "Running EF Core migrations using host connection ${MIGRATION_DB_HOST}:${MIGRATION_DB_PORT}/${DB_NAME}"
  dotnet tool restore >/dev/null
  dotnet tool run dotnet-ef database update \
    --project backend/Hop.Api/Hop.Api.csproj \
    --startup-project backend/Hop.Api/Hop.Api.csproj >/dev/null
  log "EF Core migrations completed"
}

verify_tables() {
  log "Verifying required tables"
  local tables=(
    users departments roles permissions leave_requests leave_approvals leave_balances
    leave_types leave_holidays notifications line_delivery_logs audit_logs
  )

  for table in "${tables[@]}"; do
    local exists
    exists="$(docker exec -e PGPASSWORD="$DB_PASSWORD" "$POSTGRES_CONTAINER" \
      psql -U "$DB_USER" -d "$DB_NAME" -tAc "SELECT to_regclass('public.${table}') IS NOT NULL;" 2>/dev/null | tr -d '[:space:]')"
    if [ "$exists" != "t" ]; then
      fail "Required table not found: $table"
    fi
    log "OK table: $table"
  done
}

load_env
log "Starting database deployment"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d "$POSTGRES_SERVICE"
wait_for_postgres
backup_before_migration
run_migrations
verify_tables
log "Database deployment completed"
