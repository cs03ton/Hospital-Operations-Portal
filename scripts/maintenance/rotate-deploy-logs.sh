#!/usr/bin/env bash
set -euo pipefail

DEPLOY_LOG_ROOT="${DEPLOY_LOG_ROOT:-./logs/deploy}"
BACKUP_LOG_ROOT="${BACKUP_LOG_ROOT:-./backups/logs}"
SYSTEM_LOG_ROOT="${SYSTEM_LOG_ROOT:-/var/log/hop}"
LOG_RETENTION_DAYS="${LOG_RETENTION_DAYS:-30}"

log() { printf '[%s] %s\n' "$(date -Is)" "$*"; }

rotate_dir() {
  local dir="$1"
  local pattern="$2"
  if [ ! -d "$dir" ]; then
    log "SKIP: ${dir} not found"
    return
  fi

  log "Rotating ${pattern} in ${dir}; retention=${LOG_RETENTION_DAYS} days"
  find "$dir" -type f -name "$pattern" -mtime +"$LOG_RETENTION_DAYS" -print -delete
}

rotate_dir "$DEPLOY_LOG_ROOT" "deploy_*.log"
rotate_dir "$BACKUP_LOG_ROOT" "*.log"
rotate_dir "$SYSTEM_LOG_ROOT" "*.log"

log "Log rotation completed"
