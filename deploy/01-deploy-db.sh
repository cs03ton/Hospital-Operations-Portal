#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

ENV_FILE="${ENV_FILE:-.env.production}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
POSTGRES_SERVICE="${POSTGRES_SERVICE:-postgres}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-hop-prod-postgres}"
MIGRATION_TIMEOUT_SECONDS="${MIGRATION_TIMEOUT_SECONDS:-120}"
MIGRATION_DB_HOST="${MIGRATION_DB_HOST:-127.0.0.1}"
MIGRATION_DB_PORT="${MIGRATION_DB_PORT:-${POSTGRES_PORT:-5432}}"

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
run_migrations
verify_tables
log "Database deployment completed"
