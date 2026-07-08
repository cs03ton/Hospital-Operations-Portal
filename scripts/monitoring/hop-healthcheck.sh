#!/usr/bin/env bash
set -euo pipefail

FRONTEND_URL="${FRONTEND_URL:-${PUBLIC_APP_URL:-http://localhost}}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose.prod.yml}"
ENV_FILE="${ENV_FILE:-.env.production}"
DISK_PATH="${DISK_PATH:-/}"
DISK_WARNING_PERCENT="${DISK_WARNING_PERCENT:-85}"
ALERT_WEBHOOK_URL="${ALERT_WEBHOOK_URL:-}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }

failures=()

record_failure() {
  failures+=("$1")
  log "FAIL: $1"
}

check_url() {
  local name="$1"
  local url="$2"
  if curl -fsS --max-time 10 "$url" >/dev/null; then
    log "OK: ${name}"
  else
    record_failure "${name} is not reachable (${url})"
  fi
}

check_docker_compose() {
  if [ ! -f "$ENV_FILE" ] || [ ! -f "$COMPOSE_FILE" ]; then
    log "SKIP: docker compose check requires ${ENV_FILE} and ${COMPOSE_FILE}"
    return
  fi

  if docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" ps >/dev/null 2>&1; then
    log "OK: docker compose services are queryable"
  else
    record_failure "docker compose services are not queryable"
  fi
}

check_disk() {
  local used
  used="$(df -P "$DISK_PATH" | awk 'NR==2 { gsub("%", "", $5); print $5 }')"
  if [ -z "$used" ]; then
    record_failure "disk usage could not be read for ${DISK_PATH}"
    return
  fi

  if [ "$used" -ge "$DISK_WARNING_PERCENT" ]; then
    record_failure "disk usage warning: ${used}% on ${DISK_PATH}"
  else
    log "OK: disk usage ${used}% on ${DISK_PATH}"
  fi
}

send_alert() {
  [ -n "$ALERT_WEBHOOK_URL" ] || return
  local message="$1"
  python3 - "$message" <<'PY' | curl -fsS --max-time 10 -H "Content-Type: application/json" --data @- "$ALERT_WEBHOOK_URL" >/dev/null || true
import json
import sys
print(json.dumps({"text": sys.argv[1]}, ensure_ascii=False))
PY
}

log "Starting HOP healthcheck"
check_url "frontend" "${FRONTEND_URL}/"
check_url "live health" "${FRONTEND_URL}/health/live"
check_url "ready health" "${FRONTEND_URL}/health/ready"
check_docker_compose
check_disk

if [ "${#failures[@]}" -gt 0 ]; then
  alert_message="HOP healthcheck failed: ${failures[*]}"
  send_alert "$alert_message"
  log "$alert_message"
  exit 1
fi

log "HOP healthcheck completed successfully"
