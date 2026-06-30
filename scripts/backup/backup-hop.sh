#!/usr/bin/env bash
set -Eeuo pipefail

timestamp="$(date +%Y%m%d_%H%M%S)"

BACKUP_MODE="${BACKUP_MODE:-host}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:?DB_NAME is required}"
DB_USER="${DB_USER:?DB_USER is required}"
DB_PASSWORD="${DB_PASSWORD:-}"
BACKUP_ROOT="${BACKUP_ROOT:-./backups}"
STORAGE_PATH="${STORAGE_PATH:-./storage}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-30}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-hop-prod-postgres}"
STORAGE_DOCKER_VOLUME="${STORAGE_DOCKER_VOLUME:-hop_prod_storage}"

db_dir="${BACKUP_ROOT}/db"
storage_dir="${BACKUP_ROOT}/storage"
log_dir="${BACKUP_ROOT}/logs"
run_dir="${BACKUP_ROOT}/${timestamp}"
log_file="${log_dir}/backup_${timestamp}.log"

mkdir -p "$db_dir" "$storage_dir" "$log_dir" "$run_dir"

log() {
  printf '%s %s\n' "$(date -Is)" "$*" | tee -a "$log_file"
}

fail() {
  log "ERROR: $*"
  exit 1
}

cleanup_old_backups() {
  log "Applying retention: ${BACKUP_RETENTION_DAYS} days"
  find "$db_dir" -type f -name 'hop_db_*.dump' -mtime +"$BACKUP_RETENTION_DAYS" -print -delete >>"$log_file" 2>&1 || true
  find "$storage_dir" -type f -name 'hop_storage_*.tar.gz' -mtime +"$BACKUP_RETENTION_DAYS" -print -delete >>"$log_file" 2>&1 || true
  find "$log_dir" -type f -name 'backup_*.log' -mtime +"$BACKUP_RETENTION_DAYS" -print -delete >>"$log_file" 2>&1 || true
  find "$BACKUP_ROOT" -mindepth 1 -maxdepth 1 -type d -name '20*' -mtime +"$BACKUP_RETENTION_DAYS" -print -exec rm -rf {} + >>"$log_file" 2>&1 || true
}

backup_database_host() {
  command -v pg_dump >/dev/null 2>&1 || fail "pg_dump not found. Install PostgreSQL client tools or use BACKUP_MODE=docker."
  local output_file="$db_dir/hop_db_${timestamp}.dump"
  log "Backing up PostgreSQL database in host mode to ${output_file}"
  PGPASSWORD="$DB_PASSWORD" pg_dump \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --dbname="$DB_NAME" \
    --format=custom \
    --no-owner \
    --file="$output_file" >>"$log_file" 2>&1
  cp "$output_file" "$run_dir/"
}

backup_database_docker() {
  command -v docker >/dev/null 2>&1 || fail "docker not found."
  local output_file="$db_dir/hop_db_${timestamp}.dump"
  log "Backing up PostgreSQL database from container ${POSTGRES_CONTAINER} to ${output_file}"
  docker exec \
    -e PGPASSWORD="$DB_PASSWORD" \
    "$POSTGRES_CONTAINER" \
    pg_dump \
      --host="${DB_HOST:-localhost}" \
      --port="$DB_PORT" \
      --username="$DB_USER" \
      --dbname="$DB_NAME" \
      --format=custom \
      --no-owner >"$output_file" 2>>"$log_file"
  cp "$output_file" "$run_dir/"
}

backup_storage_host() {
  local output_file="$storage_dir/hop_storage_${timestamp}.tar.gz"
  if [ ! -d "$STORAGE_PATH" ]; then
    log "Storage path not found: ${STORAGE_PATH}; creating empty storage archive"
    mkdir -p "$run_dir/empty-storage"
    tar -czf "$output_file" -C "$run_dir/empty-storage" . >>"$log_file" 2>&1
  else
    log "Backing up storage path ${STORAGE_PATH} to ${output_file}"
    tar -czf "$output_file" -C "$STORAGE_PATH" . >>"$log_file" 2>&1
  fi
  cp "$output_file" "$run_dir/"
}

backup_storage_docker() {
  command -v docker >/dev/null 2>&1 || fail "docker not found."
  local output_file="$storage_dir/hop_storage_${timestamp}.tar.gz"
  log "Backing up Docker volume ${STORAGE_DOCKER_VOLUME} to ${output_file}"
  docker run --rm \
    -v "${STORAGE_DOCKER_VOLUME}:/data:ro" \
    -v "$(cd "$storage_dir" && pwd):/backup" \
    alpine:3.20 \
    tar -czf "/backup/$(basename "$output_file")" -C /data . >>"$log_file" 2>&1
  cp "$output_file" "$run_dir/"
}

log "Starting HOP backup"
log "Mode=${BACKUP_MODE}; DB=${DB_HOST}:${DB_PORT}/${DB_NAME}; BackupRoot=${BACKUP_ROOT}; StoragePath=${STORAGE_PATH}"

case "$BACKUP_MODE" in
  host)
    backup_database_host
    backup_storage_host
    ;;
  docker)
    backup_database_docker
    backup_storage_docker
    ;;
  *)
    fail "Unsupported BACKUP_MODE: ${BACKUP_MODE}. Use host or docker."
    ;;
esac

cleanup_old_backups
log "Backup completed successfully: ${run_dir}"
