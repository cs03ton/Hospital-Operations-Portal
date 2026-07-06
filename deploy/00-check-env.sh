#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

ENV_FILE="${ENV_FILE:-.env.production}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

load_env() {
  if [ ! -f "$ENV_FILE" ]; then
    fail "$ENV_FILE not found. Copy .env.production.example to .env.production and fill required values."
  fi

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
  export LINE_ENABLED="${LINE_ENABLED:-${Line__Enabled:-false}}"
  export Line__Enabled="${Line__Enabled:-${LINE_ENABLED:-false}}"
  export Line__AccessToken="${Line__AccessToken:-${LINE_ACCESS_TOKEN:-${Line__ChannelAccessToken:-${LINE_CHANNEL_ACCESS_TOKEN:-}}}}"
  export Line__ChannelSecret="${Line__ChannelSecret:-${LINE_CHANNEL_SECRET:-}}"
  export Storage__RootPath="${Storage__RootPath:-${STORAGE_ROOT_PATH:-/app/storage}}"
  export Storage__PublicBaseUrl="${Storage__PublicBaseUrl:-${STORAGE_PUBLIC_BASE_URL:-${PUBLIC_FILE_BASE_URL:-}}}"
  export ConnectionStrings__DefaultConnection="${ConnectionStrings__DefaultConnection:-Host=${DB_HOST:-postgres};Port=${DB_PORT:-5432};Database=${DB_NAME:-};Username=${DB_USER:-};Password=${DB_PASSWORD:-}}"
}

check_command() {
  command -v "$1" >/dev/null 2>&1 || fail "$1 is not installed or not in PATH."
}

require_env() {
  local key="$1"
  local value="${!key:-}"
  if [ -z "$value" ]; then
    fail "Required environment variable is missing: $key"
  fi
  log "OK env: $key is set"
}

check_docker_compose() {
  check_command docker
  docker compose version >/dev/null 2>&1 || fail "docker compose plugin is not available. Use Docker Compose v2."
}

load_env

log "Checking HOP Phase 1 deploy prerequisites"
check_docker_compose
check_command curl

if command -v dotnet >/dev/null 2>&1; then
  log "OK command: dotnet is available"
else
  log "WARN: dotnet not found. Docker image build can still run, but EF migrations from host require dotnet SDK."
fi

if command -v npm >/dev/null 2>&1; then
  log "OK command: npm is available"
else
  log "WARN: npm not found. Frontend container build can still run, but host npm build check is skipped."
fi

[ -f "$COMPOSE_FILE" ] || fail "$COMPOSE_FILE not found."
log "OK compose file: $COMPOSE_FILE"

require_env ASPNETCORE_ENVIRONMENT
require_env ConnectionStrings__DefaultConnection
require_env Jwt__Key
require_env Line__Enabled
require_env Line__AccessToken
require_env Line__ChannelSecret
require_env PUBLIC_APP_URL
require_env Storage__RootPath
require_env Storage__PublicBaseUrl
require_env DB_HOST
require_env DB_PORT
require_env DB_NAME
require_env DB_USER
require_env DB_PASSWORD

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" config >/dev/null
log "OK docker compose config"
log "Environment check completed"
