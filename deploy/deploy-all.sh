#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

ENV_FILE="${ENV_FILE:-.env.production}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }

log "Starting HOP Phase 1 deploy"
if [ -f "$ENV_FILE" ]; then
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
fi

bash deploy/00-check-env.sh
bash deploy/01-deploy-db.sh
bash deploy/02-deploy-backend.sh
bash deploy/03-deploy-frontend.sh
bash deploy/04-crosscheck.sh

FRONTEND_URL="${FRONTEND_URL:-${PUBLIC_APP_URL:-http://localhost}}"
log "HOP Phase 1 Deploy Completed"
log "Frontend: ${FRONTEND_URL}"
log "Backend health: ${FRONTEND_URL}/health"
log "LINE Operations Center: ${FRONTEND_URL}/admin/line-settings"
