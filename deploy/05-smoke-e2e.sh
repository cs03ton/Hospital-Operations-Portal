#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

FRONTEND_URL="${FRONTEND_URL:-${PUBLIC_APP_URL:-http://localhost}}"
SMOKE_USERNAME="${SMOKE_USERNAME:-}"
SMOKE_PASSWORD="${SMOKE_PASSWORD:-}"
SMOKE_LEAVE_REQUEST_ID="${SMOKE_LEAVE_REQUEST_ID:-}"
SMOKE_EXPECT_FORBIDDEN_URL="${SMOKE_EXPECT_FORBIDDEN_URL:-}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }
fail() { log "ERROR: $*"; exit 1; }

require_command() {
  command -v "$1" >/dev/null 2>&1 || fail "$1 is required for deploy smoke test."
}

check_url() {
  local name="$1"
  local url="$2"
  curl -fsS "$url" >/dev/null || fail "$name is not reachable: $url"
  log "OK $name"
}

api_get() {
  local name="$1"
  local path="$2"
  local expected="${3:-200}"
  local tmp status
  tmp="$(mktemp)"
  status="$(curl -sS -o "$tmp" -w '%{http_code}' \
    -H "Authorization: Bearer ${access_token}" \
    -H "X-Correlation-ID: smoke-$(date +%s)" \
    "${FRONTEND_URL}${path}" || true)"
  if [ "$status" != "$expected" ]; then
    cat "$tmp" >&2 || true
    rm -f "$tmp"
    fail "$name returned HTTP ${status}; expected ${expected}"
  fi
  rm -f "$tmp"
  log "OK $name (${status})"
}

extract_access_token() {
  python3 - "$1" <<'PY'
import json
import sys
with open(sys.argv[1], encoding="utf-8") as handle:
    payload = json.load(handle)
token = payload.get("data", {}).get("accessToken")
if not token:
    raise SystemExit("accessToken missing from login response")
print(token)
PY
}

require_command curl
require_command python3

log "Starting HOP deploy smoke test for ${FRONTEND_URL}"
check_url "frontend homepage" "${FRONTEND_URL}/"
check_url "frontend dashboard route" "${FRONTEND_URL}/dashboard"
check_url "live health" "${FRONTEND_URL}/health/live"
check_url "ready health" "${FRONTEND_URL}/health/ready"

if [ -z "$SMOKE_USERNAME" ] || [ -z "$SMOKE_PASSWORD" ]; then
  fail "SMOKE_USERNAME and SMOKE_PASSWORD are required for authenticated smoke test."
fi

login_body="$(mktemp)"
login_response="$(mktemp)"
python3 - "$SMOKE_USERNAME" "$SMOKE_PASSWORD" "$login_body" <<'PY'
import json
import sys
username, password, path = sys.argv[1:4]
with open(path, "w", encoding="utf-8") as handle:
    json.dump({"username": username, "password": password}, handle)
PY

login_status="$(curl -sS -o "$login_response" -w '%{http_code}' \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: smoke-login-$(date +%s)" \
  --data @"$login_body" \
  "${FRONTEND_URL}/api/auth/login" || true)"

rm -f "$login_body"

if [ "$login_status" != "200" ]; then
  cat "$login_response" >&2 || true
  rm -f "$login_response"
  fail "Login smoke test failed with HTTP ${login_status}"
fi

access_token="$(extract_access_token "$login_response")"
rm -f "$login_response"
log "OK login smoke test for ${SMOKE_USERNAME}"

api_get "dashboard summary" "/api/dashboard/summary"
api_get "leave request list" "/api/leave-requests?page=1&pageSize=1"
api_get "pending approval queue" "/api/approvals/my-pending"

if [ -n "$SMOKE_EXPECT_FORBIDDEN_URL" ]; then
  api_get "permission denied check" "$SMOKE_EXPECT_FORBIDDEN_URL" "403"
else
  log "SMOKE_EXPECT_FORBIDDEN_URL not set; skipping permission-denied check"
fi

if [ -n "$SMOKE_LEAVE_REQUEST_ID" ]; then
  api_get "leave PDF download" "/api/leave-requests/${SMOKE_LEAVE_REQUEST_ID}/pdf"
else
  log "SMOKE_LEAVE_REQUEST_ID not set; skipping PDF download smoke test"
fi

log "HOP deploy smoke test completed"
