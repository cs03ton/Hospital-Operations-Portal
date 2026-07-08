#!/usr/bin/env bash
set -Eeuo pipefail

BACKUP_MODE="${BACKUP_MODE:-host}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:?DB_NAME is required}"
DB_USER="${DB_USER:?DB_USER is required}"
DB_PASSWORD="${DB_PASSWORD:-}"
BACKUP_ROOT="${BACKUP_ROOT:-./backups}"
STORAGE_PATH="${STORAGE_PATH:-./storage}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-hop-prod-postgres}"
STORAGE_DOCKER_VOLUME="${STORAGE_DOCKER_VOLUME:-hop_prod_storage}"
DB_DUMP_PATH="${DB_DUMP_PATH:-}"
STORAGE_ARCHIVE_PATH="${STORAGE_ARCHIVE_PATH:-}"
RESTORE_DATABASE="${RESTORE_DATABASE:-true}"
RESTORE_STORAGE="${RESTORE_STORAGE:-true}"
RESTORE_CONFIRM="${RESTORE_CONFIRM:-}"
CONFIRM_TEXT="I_UNDERSTAND_THIS_WILL_OVERWRITE_HOP"

log_dir="${BACKUP_ROOT}/logs"
timestamp="$(date +%Y%m%d_%H%M%S)"
log_file="${log_dir}/restore_${timestamp}.log"
mkdir -p "$log_dir"

log() {
  printf '%s %s\n' "$(date -Is)" "$*" | tee -a "$log_file"
}

fail() {
  log "ERROR: $*"
  exit 1
}

confirm_restore() {
  if [ "$RESTORE_CONFIRM" != "$CONFIRM_TEXT" ]; then
    cat <<EOF
WARNING: This restore can overwrite HOP database and storage data.

Set this environment variable to continue:
  RESTORE_CONFIRM=${CONFIRM_TEXT}

Never restore over production unless the maintenance window, target backup,
and rollback plan have been explicitly approved.
EOF
    exit 2
  fi
}

restore_database_host() {
  [ -n "$DB_DUMP_PATH" ] || fail "DB_DUMP_PATH is required."
  [ -f "$DB_DUMP_PATH" ] || fail "Database dump not found: ${DB_DUMP_PATH}"
  command -v pg_restore >/dev/null 2>&1 || fail "pg_restore not found. Install PostgreSQL client tools or use BACKUP_MODE=docker."

  log "Restoring PostgreSQL database in host mode from ${DB_DUMP_PATH}"
  PGPASSWORD="$DB_PASSWORD" pg_restore \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --dbname="$DB_NAME" \
    --clean \
    --if-exists \
    --no-owner \
    "$DB_DUMP_PATH" >>"$log_file" 2>&1
}

restore_database_docker() {
  [ -n "$DB_DUMP_PATH" ] || fail "DB_DUMP_PATH is required."
  [ -f "$DB_DUMP_PATH" ] || fail "Database dump not found: ${DB_DUMP_PATH}"
  command -v docker >/dev/null 2>&1 || fail "docker not found."

  log "Restoring PostgreSQL database in Docker mode from ${DB_DUMP_PATH} into ${POSTGRES_CONTAINER}"
  docker exec -i \
    -e PGPASSWORD="$DB_PASSWORD" \
    "$POSTGRES_CONTAINER" \
    pg_restore \
      --host="${DB_HOST:-localhost}" \
      --port="$DB_PORT" \
      --username="$DB_USER" \
      --dbname="$DB_NAME" \
      --clean \
      --if-exists \
      --no-owner <"$DB_DUMP_PATH" 2>>"$log_file"
}

restore_storage_host() {
  if [ -z "$STORAGE_ARCHIVE_PATH" ]; then
    log "STORAGE_ARCHIVE_PATH not provided; skipping storage restore"
    return
  fi
  [ -f "$STORAGE_ARCHIVE_PATH" ] || fail "Storage archive not found: ${STORAGE_ARCHIVE_PATH}"
  log "Restoring storage archive ${STORAGE_ARCHIVE_PATH} into ${STORAGE_PATH}"
  mkdir -p "$STORAGE_PATH"
  find "$STORAGE_PATH" -mindepth 1 -maxdepth 1 -exec rm -rf {} + >>"$log_file" 2>&1
  tar -xzf "$STORAGE_ARCHIVE_PATH" -C "$STORAGE_PATH" >>"$log_file" 2>&1
}

restore_storage_docker() {
  if [ -z "$STORAGE_ARCHIVE_PATH" ]; then
    log "STORAGE_ARCHIVE_PATH not provided; skipping storage restore"
    return
  fi
  [ -f "$STORAGE_ARCHIVE_PATH" ] || fail "Storage archive not found: ${STORAGE_ARCHIVE_PATH}"
  command -v docker >/dev/null 2>&1 || fail "docker not found."

  local archive_dir archive_name
  archive_dir="$(cd "$(dirname "$STORAGE_ARCHIVE_PATH")" && pwd)"
  archive_name="$(basename "$STORAGE_ARCHIVE_PATH")"
  log "Restoring storage archive ${STORAGE_ARCHIVE_PATH} into Docker volume ${STORAGE_DOCKER_VOLUME}"
  docker run --rm \
    -v "${STORAGE_DOCKER_VOLUME}:/data" \
    -v "${archive_dir}:/backup:ro" \
    alpine:3.20 \
    sh -c "rm -rf /data/* /data/.[!.]* /data/..?* 2>/dev/null || true; tar -xzf '/backup/${archive_name}' -C /data" >>"$log_file" 2>&1
}

confirm_restore
log "Starting HOP restore"
log "Mode=${BACKUP_MODE}; DB=${DB_HOST}:${DB_PORT}/${DB_NAME}; BackupRoot=${BACKUP_ROOT}; StoragePath=${STORAGE_PATH}; RestoreDatabase=${RESTORE_DATABASE}; RestoreStorage=${RESTORE_STORAGE}"

case "$BACKUP_MODE" in
  host)
    if [ "$RESTORE_DATABASE" = "true" ]; then
      restore_database_host
    else
      log "RESTORE_DATABASE=false; skipping database restore"
    fi
    if [ "$RESTORE_STORAGE" = "true" ]; then
      restore_storage_host
    else
      log "RESTORE_STORAGE=false; skipping storage restore"
    fi
    ;;
  docker)
    if [ "$RESTORE_DATABASE" = "true" ]; then
      restore_database_docker
    else
      log "RESTORE_DATABASE=false; skipping database restore"
    fi
    if [ "$RESTORE_STORAGE" = "true" ]; then
      restore_storage_docker
    else
      log "RESTORE_STORAGE=false; skipping storage restore"
    fi
    ;;
  *)
    fail "Unsupported BACKUP_MODE: ${BACKUP_MODE}. Use host or docker."
    ;;
esac

log "Restore completed successfully"
