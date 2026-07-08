#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$ROOT_DIR"

source scripts/deploy/load-hop-env.sh
load_hop_env

HOP_ROOT="${HOP_ROOT:-/opt/hop}"
BACKEND_TARGET="${BACKEND_TARGET:-${HOP_ROOT}/backend}"
RELEASE_ROOT="${RELEASE_ROOT:-${HOP_ROOT}/releases}"
RELEASE_ID="${RELEASE_ID:-$(date +%Y%m%d_%H%M%S)}"
RELEASE_DIR="${RELEASE_ROOT}/${RELEASE_ID}"
BACKEND_RELEASE_DIR="${RELEASE_DIR}/backend"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

command -v dotnet >/dev/null 2>&1 || fail "dotnet SDK is required."
command -v rsync >/dev/null 2>&1 || fail "rsync is required."

log "Publishing backend release ${RELEASE_ID}"
mkdir -p "$BACKEND_RELEASE_DIR" "$BACKEND_TARGET" "${HOP_ROOT}/logs" "${HOP_ROOT}/uploads"

dotnet publish backend/Hop.Api/Hop.Api.csproj \
  -c Release \
  -o "$BACKEND_RELEASE_DIR" \
  --nologo

log "Syncing backend publish output to ${BACKEND_TARGET}"
rsync -a --delete "${BACKEND_RELEASE_DIR}/" "${BACKEND_TARGET}/"

log "Backend publish completed: ${BACKEND_TARGET}"
