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
DEPLOY_LOG_ROOT="${DEPLOY_LOG_ROOT:-./logs/deploy}"
DEPLOY_LOG_RETENTION_DAYS="${DEPLOY_LOG_RETENTION_DAYS:-30}"
DEPLOY_LOG_FILE="${DEPLOY_LOG_FILE:-${DEPLOY_LOG_ROOT}/deploy_$(date +%Y%m%d_%H%M%S).log}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }

mkdir -p "$DEPLOY_LOG_ROOT"
exec > >(tee -a "$DEPLOY_LOG_FILE") 2>&1
find "$DEPLOY_LOG_ROOT" -type f -name 'deploy_*.log' -mtime +"$DEPLOY_LOG_RETENTION_DAYS" -delete >/dev/null 2>&1 || true

log "Starting HOP Phase 1 deploy"
log "Deploy log: ${DEPLOY_LOG_FILE}"
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
