#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

ENV_FILE="${ENV_FILE:-.env.production}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
FRONTEND_SERVICE="${FRONTEND_SERVICE:-frontend}"
NGINX_SERVICE="${NGINX_SERVICE:-nginx}"
FRONTEND_URL="${FRONTEND_URL:-http://localhost}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

[ -f "$ENV_FILE" ] || fail "$ENV_FILE not found."

if command -v npm >/dev/null 2>&1; then
  log "Running host frontend build verification"
  (cd frontend && npm ci && npm run build)
  test -f frontend/dist/index.html || fail "frontend/dist/index.html not found after host build"
  find frontend/dist/assets -type f | grep -q . || fail "frontend/dist/assets is empty"
else
  log "WARN: npm not found; skipping host frontend build verification and relying on Docker build"
fi

log "Building frontend image"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" build "$FRONTEND_SERVICE"

log "Starting frontend and nginx services"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d "$FRONTEND_SERVICE" "$NGINX_SERVICE"

log "Verifying frontend homepage"
curl -fsS "$FRONTEND_URL/" >/dev/null

log "Verifying SPA fallback"
curl -fsS "$FRONTEND_URL/login" >/dev/null
curl -fsS "$FRONTEND_URL/leave" >/dev/null

log "Frontend deployment completed"
