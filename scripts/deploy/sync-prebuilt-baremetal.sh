#!/usr/bin/env bash
set -euo pipefail

FRONTEND_SOURCE="${FRONTEND_SOURCE:-/home/admin/hop-frontend}"
BACKEND_SOURCE="${BACKEND_SOURCE:-/home/admin/hop-backend}"
FRONTEND_TARGET="${FRONTEND_TARGET:-/var/www/hop}"
BACKEND_TARGET="${BACKEND_TARGET:-/opt/hop/backend}"
FRONTEND_OWNER="${FRONTEND_OWNER:-hop:www-data}"
BACKEND_OWNER="${BACKEND_OWNER:-hop:hop}"
FRONTEND_MODE="${FRONTEND_MODE:-755}"
BACKEND_MODE="${BACKEND_MODE:-750}"
HOP_API_SERVICE="${HOP_API_SERVICE:-hop-api}"
NGINX_SERVICE="${NGINX_SERVICE:-nginx}"
RESTART_BACKEND="${RESTART_BACKEND:-true}"
RELOAD_NGINX="${RELOAD_NGINX:-true}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

require_dir() {
  [ -d "$1" ] || fail "Directory not found: $1"
}

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "$1 is not installed or not in PATH."
}

require_command rsync
require_command sudo

require_dir "$FRONTEND_SOURCE"
require_dir "$BACKEND_SOURCE"

[ -f "${FRONTEND_SOURCE}/index.html" ] || fail "Frontend source must contain index.html: ${FRONTEND_SOURCE}"
[ -f "${BACKEND_SOURCE}/Hop.Api.dll" ] || fail "Backend source must contain Hop.Api.dll: ${BACKEND_SOURCE}"
[ -f "${BACKEND_SOURCE}/Hop.Api.deps.json" ] || fail "Backend source is missing Hop.Api.deps.json. Use dotnet publish output, not build output: ${BACKEND_SOURCE}"
[ -f "${BACKEND_SOURCE}/Microsoft.EntityFrameworkCore.dll" ] || fail "Backend source is missing Microsoft.EntityFrameworkCore.dll. Use dotnet publish output, not a partial/manual copy: ${BACKEND_SOURCE}"
[ -f "${BACKEND_SOURCE}/Npgsql.EntityFrameworkCore.PostgreSQL.dll" ] || fail "Backend source is missing Npgsql.EntityFrameworkCore.PostgreSQL.dll. Use dotnet publish output, not a partial/manual copy: ${BACKEND_SOURCE}"
[ -f "${BACKEND_SOURCE}/SkiaSharp.dll" ] || fail "Backend source is missing SkiaSharp.dll. Rebuild/publish backend with current dependencies before deploy: ${BACKEND_SOURCE}"
[ -f "${BACKEND_SOURCE}/runtimes/linux-x64/native/libSkiaSharp.so" ] || fail "Backend source is missing runtimes/linux-x64/native/libSkiaSharp.so. Rebuild/publish backend for linux-x64 before deploy: ${BACKEND_SOURCE}"

log "Syncing frontend from ${FRONTEND_SOURCE} to ${FRONTEND_TARGET}"
sudo mkdir -p "$FRONTEND_TARGET"
sudo rsync -av --delete "${FRONTEND_SOURCE%/}/" "${FRONTEND_TARGET%/}/"
sudo chown -R "$FRONTEND_OWNER" "$FRONTEND_TARGET"
sudo chmod -R "$FRONTEND_MODE" "$FRONTEND_TARGET"

log "Syncing backend from ${BACKEND_SOURCE} to ${BACKEND_TARGET}"
sudo mkdir -p "$BACKEND_TARGET"
sudo rsync -av --delete "${BACKEND_SOURCE%/}/" "${BACKEND_TARGET%/}/"
sudo chown -R "$BACKEND_OWNER" "$BACKEND_TARGET"
sudo chmod -R "$BACKEND_MODE" "$BACKEND_TARGET"

if [ "$RESTART_BACKEND" = "true" ]; then
  log "Restarting ${HOP_API_SERVICE}"
  sudo systemctl restart "$HOP_API_SERVICE"
  sudo systemctl status "$HOP_API_SERVICE" --no-pager --lines=20
else
  log "RESTART_BACKEND=false; skipping backend restart"
fi

if [ "$RELOAD_NGINX" = "true" ]; then
  log "Reloading ${NGINX_SERVICE}"
  sudo nginx -t
  sudo systemctl reload "$NGINX_SERVICE"
else
  log "RELOAD_NGINX=false; skipping nginx reload"
fi

log "Prebuilt bare-metal sync completed"
