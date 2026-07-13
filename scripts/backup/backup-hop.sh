#!/usr/bin/env bash
set -Eeuo pipefail

PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
umask 077

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
DEFAULT_ENV_FILE="/etc/hop/backup.env"

DRY_RUN=false
if [ "${1:-}" = "--dry-run" ]; then
  DRY_RUN=true
fi

if [ -n "${BACKUP_ENV_FILE:-}" ] && [ -f "$BACKUP_ENV_FILE" ]; then
  # shellcheck disable=SC1090
  . "$BACKUP_ENV_FILE"
elif [ -f "$DEFAULT_ENV_FILE" ]; then
  # shellcheck disable=SC1090
  . "$DEFAULT_ENV_FILE"
fi

timestamp="$(date +%Y%m%d_%H%M%S)"

BACKUP_MODE="${BACKUP_MODE:-host}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:?DB_NAME is required}"
DB_USER="${DB_USER:?DB_USER is required}"
DB_PASSWORD="${DB_PASSWORD:-}"
BACKUP_ROOT="${BACKUP_ROOT:-/opt/hop/backups}"
STORAGE_PATH="${UPLOADS_PATH:-${STORAGE_PATH:-${PROJECT_ROOT}/storage}}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-7}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-hop-prod-postgres}"
STORAGE_DOCKER_VOLUME="${STORAGE_DOCKER_VOLUME:-hop_prod_storage}"
LOCK_FILE="${LOCK_FILE:-/tmp/hop-backup.lock}"

db_dir="${BACKUP_ROOT}/postgres"
storage_dir="${BACKUP_ROOT}/storage"
log_dir="${BACKUP_ROOT}/logs"
run_dir="${BACKUP_ROOT}/${timestamp}"
log_file="${LOG_FILE:-${log_dir}/backup_${timestamp}.log}"

mkdir -p "$db_dir" "$storage_dir" "$log_dir" "$run_dir" "$(dirname "$LOCK_FILE")"
chmod 700 "$BACKUP_ROOT" "$db_dir" "$storage_dir" "$log_dir" "$run_dir" 2>/dev/null || true

log() {
  printf '%s %s\n' "$(date -Is)" "$*" | tee -a "$log_file"
}

fail() {
  log "ERROR: $*"
  exit 1
}

validate_positive_integer() {
  case "$1" in
    ''|*[!0-9]*) fail "$2 must be a positive integer." ;;
  esac
  [ "$1" -gt 0 ] || fail "$2 must be greater than 0."
}

assert_non_empty_file() {
  local path="$1"
  [ -f "$path" ] || fail "Expected file was not created: $path"
  [ -s "$path" ] || fail "Expected file is empty: $path"
}

cleanup_failed_file() {
  local path="$1"
  if [ -n "$path" ] && [ -f "$path" ]; then
    rm -f -- "$path"
  fi
}

check_docker_container() {
  command -v docker >/dev/null 2>&1 || fail "docker not found."
  docker inspect "$POSTGRES_CONTAINER" >/dev/null 2>&1 || fail "PostgreSQL container not found: $POSTGRES_CONTAINER"
  docker inspect -f '{{.State.Running}}' "$POSTGRES_CONTAINER" 2>/dev/null | grep -q true || fail "PostgreSQL container is not running: $POSTGRES_CONTAINER"
}

check_database_connection_host() {
  command -v pg_isready >/dev/null 2>&1 || fail "pg_isready not found. Install PostgreSQL client tools or use BACKUP_MODE=docker."
  PGPASSWORD="$DB_PASSWORD" pg_isready --host="$DB_HOST" --port="$DB_PORT" --username="$DB_USER" --dbname="$DB_NAME" >>"$log_file" 2>&1 || fail "PostgreSQL is not ready."
}

check_database_connection_docker() {
  check_docker_container
  docker exec -e PGPASSWORD="$DB_PASSWORD" "$POSTGRES_CONTAINER" pg_isready --host="${DB_HOST:-localhost}" --port="$DB_PORT" --username="$DB_USER" --dbname="$DB_NAME" >>"$log_file" 2>&1 || fail "PostgreSQL container is not ready."
}

cleanup_old_backups() {
  validate_positive_integer "$BACKUP_RETENTION_DAYS" "BACKUP_RETENTION_DAYS"
  log "Applying retention: ${BACKUP_RETENTION_DAYS} days"
  if [ "$DRY_RUN" = "true" ]; then
    find "$db_dir" -type f \( -name 'hopdb_*.backup' -o -name 'hop_db_*.dump' \) -mtime +"$BACKUP_RETENTION_DAYS" -print >>"$log_file" 2>&1 || true
    find "$storage_dir" -type f \( -name 'hop_storage_*.tar.gz' -o -name 'hop_uploads_*.tar.gz' \) -mtime +"$BACKUP_RETENTION_DAYS" -print >>"$log_file" 2>&1 || true
    find "$log_dir" -type f \( -name 'backup_*.log' -o -name 'restore_*.log' \) -mtime +"$BACKUP_RETENTION_DAYS" -print >>"$log_file" 2>&1 || true
    return
  fi

  find "$db_dir" -type f \( -name 'hopdb_*.backup' -o -name 'hop_db_*.dump' \) -mtime +"$BACKUP_RETENTION_DAYS" -print -delete >>"$log_file" 2>&1 || true
  find "$storage_dir" -type f \( -name 'hop_storage_*.tar.gz' -o -name 'hop_uploads_*.tar.gz' \) -mtime +"$BACKUP_RETENTION_DAYS" -print -delete >>"$log_file" 2>&1 || true
  find "$log_dir" -type f \( -name 'backup_*.log' -o -name 'restore_*.log' \) -mtime +"$BACKUP_RETENTION_DAYS" -print -delete >>"$log_file" 2>&1 || true
}

backup_database_host() {
  local output_file="$db_dir/hopdb_${timestamp}.backup"
  log "Backing up PostgreSQL database in host mode to ${output_file}"
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: pg_dump custom archive would be written to ${output_file}"
    return
  fi
  command -v pg_dump >/dev/null 2>&1 || fail "pg_dump not found. Install PostgreSQL client tools or use BACKUP_MODE=docker."
  check_database_connection_host

  if ! PGPASSWORD="$DB_PASSWORD" pg_dump \
    --host="$DB_HOST" \
    --port="$DB_PORT" \
    --username="$DB_USER" \
    --dbname="$DB_NAME" \
    --format=custom \
    --no-owner \
    --file="$output_file" >>"$log_file" 2>&1; then
    cleanup_failed_file "$output_file"
    fail "pg_dump failed."
  fi
  assert_non_empty_file "$output_file"
  cp "$output_file" "$run_dir/"
}

backup_database_docker() {
  local output_file="$db_dir/hopdb_${timestamp}.backup"
  log "Backing up PostgreSQL database from container ${POSTGRES_CONTAINER} to ${output_file}"
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: docker pg_dump custom archive would be written to ${output_file}"
    return
  fi
  command -v docker >/dev/null 2>&1 || fail "docker not found."
  check_database_connection_docker

  if ! docker exec \
    -e PGPASSWORD="$DB_PASSWORD" \
    "$POSTGRES_CONTAINER" \
    pg_dump \
      --host="${DB_HOST:-localhost}" \
      --port="$DB_PORT" \
      --username="$DB_USER" \
      --dbname="$DB_NAME" \
      --format=custom \
      --no-owner >"$output_file" 2>>"$log_file"; then
    cleanup_failed_file "$output_file"
    fail "docker pg_dump failed."
  fi
  assert_non_empty_file "$output_file"
  cp "$output_file" "$run_dir/"
}

backup_storage_host() {
  local output_file="$storage_dir/hop_uploads_${timestamp}.tar.gz"
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: storage archive would be written to ${output_file} from ${STORAGE_PATH}"
    return
  fi

  if [ ! -d "$STORAGE_PATH" ]; then
    log "Storage path not found: ${STORAGE_PATH}; creating empty storage archive"
    mkdir -p "$run_dir/empty-storage"
    tar -czf "$output_file" -C "$run_dir/empty-storage" . >>"$log_file" 2>&1 || {
      cleanup_failed_file "$output_file"
      fail "empty storage archive failed."
    }
  else
    log "Backing up storage/uploads path ${STORAGE_PATH} to ${output_file}"
    tar -czf "$output_file" -C "$STORAGE_PATH" . >>"$log_file" 2>&1 || {
      cleanup_failed_file "$output_file"
      fail "storage archive failed."
    }
  fi
  assert_non_empty_file "$output_file"
  cp "$output_file" "$run_dir/"
}

backup_storage_docker() {
  local output_file="$storage_dir/hop_uploads_${timestamp}.tar.gz"
  log "Backing up Docker volume ${STORAGE_DOCKER_VOLUME} to ${output_file}"
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: docker volume ${STORAGE_DOCKER_VOLUME} archive would be written to ${output_file}"
    return
  fi
  command -v docker >/dev/null 2>&1 || fail "docker not found."

  if ! docker run --rm \
    -v "${STORAGE_DOCKER_VOLUME}:/data:ro" \
    -v "$(cd "$storage_dir" && pwd):/backup" \
    alpine:3.20 \
    tar -czf "/backup/$(basename "$output_file")" -C /data . >>"$log_file" 2>&1; then
    cleanup_failed_file "$output_file"
    fail "docker storage archive failed."
  fi
  assert_non_empty_file "$output_file"
  cp "$output_file" "$run_dir/"
}

main() {
  validate_positive_integer "$BACKUP_RETENTION_DAYS" "BACKUP_RETENTION_DAYS"
  log "Starting HOP backup"
  log "Mode=${BACKUP_MODE}; DB=${DB_HOST}:${DB_PORT}/${DB_NAME}; BackupRoot=${BACKUP_ROOT}; StoragePath=${STORAGE_PATH}; RetentionDays=${BACKUP_RETENTION_DAYS}; DryRun=${DRY_RUN}"

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
}

(
  flock -n 9 || fail "Another HOP backup process is already running."
  main
) 9>"$LOCK_FILE"
