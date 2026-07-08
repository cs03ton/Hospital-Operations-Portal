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
DEPLOY_MODE="${DEPLOY_MODE:-${HOP_DEPLOY_MODE:-}}"
if [ -z "$DEPLOY_MODE" ]; then
  if [ -f "$DEFAULT_ENV_FILE" ]; then
    DEPLOY_MODE="baremetal"
  else
    DEPLOY_MODE="docker"
  fi
fi

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

load_env() {
  if [ ! -f "$ENV_FILE" ]; then
    fail "$ENV_FILE not found. Create /etc/hop/hop-api.env or copy .env.production.example to .env.production and fill required values."
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
  if [ "$DEPLOY_MODE" = "baremetal" ]; then
    export Storage__RootPath="${Storage__RootPath:-${STORAGE_ROOT_PATH:-${STORAGE_PATH:-/opt/hop/uploads}}}"
  else
    export Storage__RootPath="${Storage__RootPath:-${STORAGE_ROOT_PATH:-/app/storage}}"
  fi
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

check_baremetal_commands() {
  check_command curl
  check_command dotnet
  check_command npm
  check_command rsync
  check_command systemctl
  check_command nginx
}

check_baremetal_paths() {
  local hop_root="${HOP_ROOT:-/opt/hop}"
  local frontend_target="${FRONTEND_TARGET:-/var/www/hop}"

  for path in "$hop_root" "${hop_root}/backend" "${hop_root}/uploads" "${hop_root}/logs" "${hop_root}/backups" "${hop_root}/releases" "$frontend_target"; do
    if [ -d "$path" ]; then
      log "OK path: $path"
    else
      log "WARN: path does not exist yet: $path"
    fi
  done
}

load_env

log "Checking HOP Phase 1 deploy prerequisites (${DEPLOY_MODE})"

case "$DEPLOY_MODE" in
  baremetal)
    check_baremetal_commands
    check_baremetal_paths
    ;;
  docker)
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
    ;;
  *)
    fail "Unsupported DEPLOY_MODE: ${DEPLOY_MODE}. Use baremetal or docker."
    ;;
esac

require_env ASPNETCORE_ENVIRONMENT
require_env ConnectionStrings__DefaultConnection
require_env Jwt__Key
require_env Line__Enabled
if [ "$Line__Enabled" = "true" ]; then
  require_env Line__AccessToken
  require_env Line__ChannelSecret
else
  log "LINE disabled; skipping LINE secret checks"
fi
require_env PUBLIC_APP_URL
require_env Storage__RootPath
require_env Storage__PublicBaseUrl
require_env DB_HOST
require_env DB_PORT
require_env DB_NAME
require_env DB_USER
require_env DB_PASSWORD

if [ "$DEPLOY_MODE" = "docker" ]; then
  docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" config >/dev/null
  log "OK docker compose config"
else
  log "Bare-metal mode; skipping docker compose config"
fi
log "Environment check completed"
