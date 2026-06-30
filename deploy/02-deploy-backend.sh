#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

ENV_FILE="${ENV_FILE:-.env.production}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
BACKEND_SERVICE="${BACKEND_SERVICE:-backend}"
BACKEND_CONTAINER="${BACKEND_CONTAINER:-hop-prod-api}"
HEALTH_TIMEOUT_SECONDS="${HEALTH_TIMEOUT_SECONDS:-120}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

[ -f "$ENV_FILE" ] || fail "$ENV_FILE not found."

log "Building backend image"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" build "$BACKEND_SERVICE"

log "Starting backend service"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d "$BACKEND_SERVICE"

log "Waiting for backend health inside container"
end=$((SECONDS + HEALTH_TIMEOUT_SECONDS))
until docker exec "$BACKEND_CONTAINER" curl -fsS http://localhost:8080/healthz >/dev/null 2>&1 || docker exec "$BACKEND_CONTAINER" curl -fsS http://localhost:8080/api/health >/dev/null 2>&1; do
  if [ "$SECONDS" -ge "$end" ]; then
    docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" logs --tail=80 "$BACKEND_SERVICE" || true
    fail "Backend health check failed inside container"
  fi
  sleep 3
done
log "Backend health check passed"

log "Checking storage path writability inside backend container"
docker exec "$BACKEND_CONTAINER" sh -c 'test -w /app/storage && touch /app/storage/.deploy-write-test && rm -f /app/storage/.deploy-write-test'
log "Storage path is writable"

log "Checking LINE config status without printing secrets"
docker exec "$BACKEND_CONTAINER" sh -c 'if [ "${Line__Enabled:-false}" = "true" ]; then test -n "${Line__ChannelAccessToken:-}" && test -n "${Line__ChannelSecret:-}"; else exit 0; fi'
log "LINE config check completed"
log "Backend deployment completed"
