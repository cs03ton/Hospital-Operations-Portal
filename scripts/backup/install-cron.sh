#!/usr/bin/env bash
set -Eeuo pipefail

PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKUP_SCRIPT="${BACKUP_SCRIPT:-${SCRIPT_DIR}/backup-hop.sh}"
BACKUP_ENV_FILE="${BACKUP_ENV_FILE:-/etc/hop/backup.env}"
CRON_SCHEDULE="${CRON_SCHEDULE:-0 2 * * *}"
CRON_LOG_FILE="${CRON_LOG_FILE:-/var/log/hop-backup-cron.log}"
CRON_MARKER="# HOP_BACKUP_CRON"

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

[ -f "$BACKUP_SCRIPT" ] || fail "Backup script not found: $BACKUP_SCRIPT"
[ -x "$BACKUP_SCRIPT" ] || fail "Backup script is not executable: chmod +x $BACKUP_SCRIPT"
[ -f "$BACKUP_ENV_FILE" ] || fail "Backup env file not found: $BACKUP_ENV_FILE"

mkdir -p "$(dirname "$CRON_LOG_FILE")"
touch "$CRON_LOG_FILE" || fail "Cannot write cron log file: $CRON_LOG_FILE"

if grep -E '^LOG_FILE=' "$BACKUP_ENV_FILE" >/dev/null 2>&1; then
  configured_log_file="$(grep -E '^LOG_FILE=' "$BACKUP_ENV_FILE" | tail -1 | cut -d= -f2-)"
  if [ -n "$configured_log_file" ]; then
    mkdir -p "$(dirname "$configured_log_file")" 2>/dev/null || true
    touch "$configured_log_file" 2>/dev/null || printf 'WARN: Cannot write configured LOG_FILE=%s. backup-hop.sh will fall back to BACKUP_ROOT/logs.\n' "$configured_log_file" >&2
  fi
fi

backup_abs="$(cd "$(dirname "$BACKUP_SCRIPT")" && pwd)/$(basename "$BACKUP_SCRIPT")"
cron_line="${CRON_SCHEDULE} BACKUP_ENV_FILE=${BACKUP_ENV_FILE} ${backup_abs} >> ${CRON_LOG_FILE} 2>&1 ${CRON_MARKER}"

tmp_file="$(mktemp)"
trap 'rm -f "$tmp_file"' EXIT

if crontab -l 2>/dev/null | grep -F "$CRON_MARKER" >/dev/null; then
  crontab -l 2>/dev/null | grep -Fv "$CRON_MARKER" >"$tmp_file"
else
  crontab -l 2>/dev/null >"$tmp_file" || true
fi

printf '%s\n' "$cron_line" >>"$tmp_file"
crontab "$tmp_file"

printf 'Installed HOP backup cron:\n'
crontab -l | grep -F "$CRON_MARKER"
