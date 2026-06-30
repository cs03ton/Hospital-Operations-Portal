#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

ENV_FILE="${ENV_FILE:-.env.production}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost}"
BACKEND_HEALTH_URL="${BACKEND_HEALTH_URL:-${FRONTEND_URL}/health}"
BACKEND_API_HEALTH_URL="${BACKEND_API_HEALTH_URL:-${FRONTEND_URL}/api/health}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-hop-prod-postgres}"
BACKEND_CONTAINER="${BACKEND_CONTAINER:-hop-prod-api}"
LINE_SETTINGS_URL="${LINE_SETTINGS_URL:-${FRONTEND_URL}/api/admin/line/settings}"

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
  export LINE_ENABLED="${LINE_ENABLED:-${Line__Enabled:-false}}"
}

check_url() {
  local name="$1"
  local url="$2"
  curl -fsS "$url" >/dev/null || fail "$name is not reachable: $url"
  log "OK URL: $name"
}

check_no_frontend_secret_leak() {
  log "Checking frontend dist for secret leakage markers"
  if [ -d frontend/dist ]; then
    if grep -R -I -E 'Line__ChannelAccessToken|Line__ChannelSecret|POSTGRES_PASSWORD|Jwt__Key|change-this-jwt-secret|change-this-strong-postgres-password' frontend/dist >/dev/null 2>&1; then
      fail "Potential secret marker found in frontend/dist"
    fi
  fi
  log "OK frontend dist secret marker check"
}

check_line_settings_endpoint() {
  log "Checking LINE settings endpoint does not expose secret names on unauthenticated response"
  local tmp
  tmp="$(mktemp)"
  status="$(curl -sS -o "$tmp" -w '%{http_code}' "$LINE_SETTINGS_URL" || true)"
  if [ "$status" = "200" ]; then
    if grep -E 'channelAccessToken|accessToken|channelSecret' "$tmp" >/dev/null 2>&1; then
      rm -f "$tmp"
      fail "LINE settings endpoint response includes raw secret fields"
    fi
    log "OK LINE settings endpoint reachable without raw secret fields"
  elif [ "$status" = "401" ] || [ "$status" = "403" ]; then
    log "OK LINE settings endpoint is protected (${status}); authenticated masked check should be done manually from LINE Operations Center"
  else
    rm -f "$tmp"
    fail "LINE settings endpoint returned unexpected HTTP status: ${status}"
  fi
  rm -f "$tmp"
}

load_env
log "Starting HOP crosscheck"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" ps >/dev/null

check_url "backend /health" "$BACKEND_HEALTH_URL"
check_url "backend /api/health" "$BACKEND_API_HEALTH_URL"
check_url "frontend homepage" "$FRONTEND_URL/"
check_url "frontend /login SPA fallback" "$FRONTEND_URL/login"
check_url "frontend /leave SPA fallback" "$FRONTEND_URL/leave"

docker exec -e PGPASSWORD="$DB_PASSWORD" "$POSTGRES_CONTAINER" pg_isready -U "$DB_USER" -d "$DB_NAME" >/dev/null
log "OK database reachable"

docker exec "$BACKEND_CONTAINER" sh -c 'test -w /app/storage'
log "OK storage writable"

check_no_frontend_secret_leak
check_line_settings_endpoint

if [ "$LINE_ENABLED" = "true" ]; then
  docker exec "$BACKEND_CONTAINER" sh -c 'test -n "${Line__ChannelAccessToken:-}" && test -n "${Line__ChannelSecret:-}"'
  log "OK LINE enabled and secret presence verified without printing values"
else
  log "LINE disabled; skipping LINE token presence check"
fi

log "HOP crosscheck completed"
