#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

ENV_FILE="${ENV_FILE:-.env.production}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
ROLLBACK_REF="${ROLLBACK_REF:-}"
ROLLBACK_CONFIRM="${ROLLBACK_CONFIRM:-}"
CONFIRM_TEXT="I_UNDERSTAND_ROLLBACK_WILL_REDEPLOY_APP"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

[ -f "$ENV_FILE" ] || fail "$ENV_FILE not found."

if [ "$ROLLBACK_CONFIRM" != "$CONFIRM_TEXT" ]; then
  cat <<EOF
WARNING: This rollback redeploys the application code/container images.

It does not automatically rollback the database. If a migration was destructive,
use scripts/backup/restore-hop.sh only after explicit production approval.

Set this environment variable to continue:
  ROLLBACK_CONFIRM=${CONFIRM_TEXT}
EOF
  exit 2
fi

if [ -n "$ROLLBACK_REF" ]; then
  log "Checking out rollback ref: ${ROLLBACK_REF}"
  git checkout "$ROLLBACK_REF"
else
  log "ROLLBACK_REF not provided; redeploying current checkout"
fi

log "Rebuilding and restarting application services"
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" build backend frontend
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d backend frontend nginx

log "Running crosscheck after rollback"
bash deploy/04-crosscheck.sh
log "Rollback completed"
