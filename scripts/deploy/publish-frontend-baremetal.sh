#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

source scripts/deploy/load-hop-env.sh
load_hop_env

HOP_ROOT="${HOP_ROOT:-/opt/hop}"
FRONTEND_TARGET="${FRONTEND_TARGET:-/var/www/hop}"
RELEASE_ROOT="${RELEASE_ROOT:-${HOP_ROOT}/releases}"
RELEASE_ID="${RELEASE_ID:-$(date +%Y%m%d_%H%M%S)}"
RELEASE_DIR="${RELEASE_ROOT}/${RELEASE_ID}"
FRONTEND_RELEASE_DIR="${RELEASE_DIR}/frontend"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

command -v npm >/dev/null 2>&1 || fail "npm is required."
command -v rsync >/dev/null 2>&1 || fail "rsync is required."

export VITE_API_BASE_URL="${VITE_API_BASE_URL:-}"
export VITE_AUTH_TOKEN_STORAGE_MODE="${VITE_AUTH_TOKEN_STORAGE_MODE:-cookie}"
export VITE_AUTH_CSRF_COOKIE_NAME="${VITE_AUTH_CSRF_COOKIE_NAME:-hop_csrf_token}"
export VITE_AUTH_CSRF_HEADER_NAME="${VITE_AUTH_CSRF_HEADER_NAME:-X-CSRF-TOKEN}"

log "Building frontend release ${RELEASE_ID}"
(cd frontend && npm ci && npm run build)

mkdir -p "$FRONTEND_RELEASE_DIR" "$FRONTEND_TARGET"
rsync -a --delete frontend/dist/ "$FRONTEND_RELEASE_DIR/"

log "Syncing frontend static files to ${FRONTEND_TARGET}"
rsync -a --delete "${FRONTEND_RELEASE_DIR}/" "${FRONTEND_TARGET}/"

log "Frontend publish completed: ${FRONTEND_TARGET}"
