#!/usr/bin/env bash
set -Eeuo pipefail

PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin
umask 077

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
DEFAULT_ENV_FILE="/etc/hop/backup.env"

ASSUME_YES=false
LIST_ONLY=false
DRY_RUN=false
BACKUP_ID=""
TARGET_DB_OVERRIDE=""
RESTORE_DATABASE_OVERRIDE=""
RESTORE_STORAGE_OVERRIDE=""

while [ "$#" -gt 0 ]; do
  case "$1" in
    --yes)
      ASSUME_YES=true
      ;;
    --list)
      LIST_ONLY=true
      ;;
    --dry-run)
      DRY_RUN=true
      ;;
    --backup-id)
      shift
      BACKUP_ID="${1:?--backup-id requires a value}"
      ;;
    --db-only)
      RESTORE_DATABASE_OVERRIDE=true
      RESTORE_STORAGE_OVERRIDE=false
      ;;
    --storage-only)
      RESTORE_DATABASE_OVERRIDE=false
      RESTORE_STORAGE_OVERRIDE=true
      ;;
    --target-db)
      shift
      TARGET_DB_OVERRIDE="${1:?--target-db requires a database name}"
      ;;
    --dump)
      shift
      DB_DUMP_PATH="${1:?--dump requires a path}"
      ;;
    --storage)
      shift
      STORAGE_ARCHIVE_PATH="${1:?--storage requires a path}"
      ;;
    *)
      printf 'Unknown option: %s\n' "$1" >&2
      exit 2
      ;;
  esac
  shift
done

if [ -n "${BACKUP_ENV_FILE:-}" ] && [ -f "$BACKUP_ENV_FILE" ]; then
  # shellcheck disable=SC1090
  . "$BACKUP_ENV_FILE"
elif [ -f "$DEFAULT_ENV_FILE" ]; then
  # shellcheck disable=SC1090
  . "$DEFAULT_ENV_FILE"
fi

BACKUP_MODE="${BACKUP_MODE:-host}"
DB_HOST="${DB_HOST:-localhost}"
DB_PORT="${DB_PORT:-5432}"
DB_NAME="${DB_NAME:-}"
DB_USER="${DB_USER:-}"
DB_PASSWORD="${DB_PASSWORD:-}"
BACKUP_ROOT="${BACKUP_ROOT:-/opt/hop/backups}"
STORAGE_PATH="${UPLOADS_PATH:-${STORAGE_PATH:-${PROJECT_ROOT}/storage}}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-hop-prod-postgres}"
STORAGE_DOCKER_VOLUME="${STORAGE_DOCKER_VOLUME:-hop_prod_storage}"
RESTORE_DATABASE="${RESTORE_DATABASE:-true}"
RESTORE_STORAGE="${RESTORE_STORAGE:-true}"
RESTORE_CONFIRMATION="${RESTORE_CONFIRMATION:-}"
CONFIRM_TEXT="RESTORE_HOP_DATABASE"

if [ -n "$TARGET_DB_OVERRIDE" ]; then
  DB_NAME="$TARGET_DB_OVERRIDE"
fi
if [ -n "$RESTORE_DATABASE_OVERRIDE" ]; then
  RESTORE_DATABASE="$RESTORE_DATABASE_OVERRIDE"
fi
if [ -n "$RESTORE_STORAGE_OVERRIDE" ]; then
  RESTORE_STORAGE="$RESTORE_STORAGE_OVERRIDE"
fi

db_dir="${BACKUP_ROOT}/postgres"
storage_dir="${BACKUP_ROOT}/storage"
log_dir="${BACKUP_ROOT}/logs"
timestamp="$(date +%Y%m%d_%H%M%S)"
log_file="${LOG_FILE:-${log_dir}/restore_${timestamp}.log}"
mkdir -p "$log_dir"

log() {
  printf '%s %s\n' "$(date -Is)" "$*" | tee -a "$log_file"
}

fail() {
  log "ERROR: $*"
  exit 1
}

human_size() {
  if command -v numfmt >/dev/null 2>&1; then
    numfmt --to=iec --suffix=B "$1"
  else
    printf '%s bytes' "$1"
  fi
}

list_backups() {
  printf 'Database backups in %s\n' "$db_dir"
  local db_rows storage_rows
  db_rows="$(find "$db_dir" -maxdepth 1 -type f \( -name 'hopdb_*.backup' -o -name 'hop_db_*.dump' \) -printf '%T@ %p %s\n' 2>/dev/null | sort -nr || true)"
  if [ -n "$db_rows" ]; then
    printf '%s\n' "$db_rows" | awk '{ size=$3; $1=""; $3=""; sub(/^  /,""); printf "- %s (%s bytes)\n", $0, size }'
  else
    printf '%s\n' '- no database backups found'
  fi

  printf '\nStorage backups in %s\n' "$storage_dir"
  storage_rows="$(find "$storage_dir" -maxdepth 1 -type f \( -name 'hop_uploads_*.tar.gz' -o -name 'hop_storage_*.tar.gz' \) -printf '%T@ %p %s\n' 2>/dev/null | sort -nr || true)"
  if [ -n "$storage_rows" ]; then
    printf '%s\n' "$storage_rows" | awk '{ size=$3; $1=""; $3=""; sub(/^  /,""); printf "- %s (%s bytes)\n", $0, size }'
  else
    printf '%s\n' '- no storage backups found'
  fi
}

latest_db_dump() {
  find "$db_dir" -maxdepth 1 -type f \( -name 'hopdb_*.backup' -o -name 'hop_db_*.dump' \) -printf '%T@ %p\n' 2>/dev/null |
    sort -nr |
    awk 'NR==1 { $1=""; sub(/^ /,""); print; exit }'
}

timestamp_from_db_dump() {
  basename "$1" | sed -n -e 's/^hopdb_\([0-9]\{8\}_[0-9]\{6\}\)\.backup$/\1/p' -e 's/^hop_db_\([0-9]\{8\}_[0-9]\{6\}\)\.dump$/\1/p'
}

matching_storage_archive() {
  local ts="$1"
  if [ -f "$storage_dir/hop_uploads_${ts}.tar.gz" ]; then
    printf '%s\n' "$storage_dir/hop_uploads_${ts}.tar.gz"
  elif [ -f "$storage_dir/hop_storage_${ts}.tar.gz" ]; then
    printf '%s\n' "$storage_dir/hop_storage_${ts}.tar.gz"
  fi
}

assert_readable_non_empty_file() {
  local path="$1"
  [ -n "$path" ] || fail "Required file path is empty."
  [ -f "$path" ] || fail "File not found: $path"
  [ -r "$path" ] || fail "File is not readable: $path"
  [ -s "$path" ] || fail "File is empty: $path"
}

check_docker_container() {
  command -v docker >/dev/null 2>&1 || fail "docker not found."
  docker inspect "$POSTGRES_CONTAINER" >/dev/null 2>&1 || fail "PostgreSQL container not found: $POSTGRES_CONTAINER"
  docker inspect -f '{{.State.Running}}' "$POSTGRES_CONTAINER" 2>/dev/null | grep -q true || fail "PostgreSQL container is not running: $POSTGRES_CONTAINER"
}

verify_archive_host() {
  command -v pg_restore >/dev/null 2>&1 || fail "pg_restore not found. Install PostgreSQL client tools or use BACKUP_MODE=docker."
  pg_restore --list "$DB_DUMP_PATH" >>"$log_file" 2>&1 || fail "pg_restore --list failed. Backup archive is not valid."
}

verify_archive_docker() {
  check_docker_container
  docker run --rm -v "$(cd "$(dirname "$DB_DUMP_PATH")" && pwd):/backup:ro" postgres:16 pg_restore --list "/backup/$(basename "$DB_DUMP_PATH")" >>"$log_file" 2>&1 ||
    fail "docker pg_restore --list failed. Backup archive is not valid."
}

confirm_restore() {
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: restore confirmation skipped"
    return
  fi

  if [ "$ASSUME_YES" = "true" ]; then
    [ "$RESTORE_CONFIRMATION" = "$CONFIRM_TEXT" ] || fail "RESTORE_CONFIRMATION=${CONFIRM_TEXT} is required with --yes."
    return
  fi

  cat <<EOF
WARNING: This restore can overwrite the current HOP database and storage.

Database backup:
  $DB_DUMP_PATH

Storage backup:
  ${STORAGE_ARCHIVE_PATH:-"(not found / skipped)"}

Type the database name "$DB_NAME" to continue:
EOF
  read -r typed_database
  [ "$typed_database" = "$DB_NAME" ] || fail "Restore cancelled because database confirmation did not match."
}

restore_database_host() {
  verify_archive_host
  log "Restoring PostgreSQL database in host mode from ${DB_DUMP_PATH}"
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: pg_restore would target ${DB_HOST}:${DB_PORT}/${DB_NAME}"
    return
  fi
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
  verify_archive_docker
  log "Restoring PostgreSQL database in Docker mode from ${DB_DUMP_PATH} into ${POSTGRES_CONTAINER}"
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: docker pg_restore would target container ${POSTGRES_CONTAINER}, database ${DB_NAME}"
    return
  fi
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

validate_restore_target_path() {
  case "$STORAGE_PATH" in
    ""|"/"|"/."|"/.."|"/var"|"/opt"|"/home")
      fail "Unsafe STORAGE_PATH for restore: ${STORAGE_PATH}"
      ;;
  esac
}

restore_storage_host() {
  if [ -z "${STORAGE_ARCHIVE_PATH:-}" ]; then
    log "WARNING: Storage archive not provided/found; skipping storage restore"
    return
  fi
  assert_readable_non_empty_file "$STORAGE_ARCHIVE_PATH"
  validate_restore_target_path
  log "Restoring storage archive ${STORAGE_ARCHIVE_PATH} into ${STORAGE_PATH}"
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: storage archive would be extracted into ${STORAGE_PATH}"
    return
  fi
  mkdir -p "$STORAGE_PATH"
  find "$STORAGE_PATH" -mindepth 1 -maxdepth 1 -exec rm -rf -- {} + >>"$log_file" 2>&1
  tar -xzf "$STORAGE_ARCHIVE_PATH" -C "$STORAGE_PATH" >>"$log_file" 2>&1
}

restore_storage_docker() {
  if [ -z "${STORAGE_ARCHIVE_PATH:-}" ]; then
    log "WARNING: Storage archive not provided/found; skipping storage restore"
    return
  fi
  assert_readable_non_empty_file "$STORAGE_ARCHIVE_PATH"
  command -v docker >/dev/null 2>&1 || fail "docker not found."

  local archive_dir archive_name
  archive_dir="$(cd "$(dirname "$STORAGE_ARCHIVE_PATH")" && pwd)"
  archive_name="$(basename "$STORAGE_ARCHIVE_PATH")"
  log "Restoring storage archive ${STORAGE_ARCHIVE_PATH} into Docker volume ${STORAGE_DOCKER_VOLUME}"
  if [ "$DRY_RUN" = "true" ]; then
    log "DRY-RUN: storage archive ${archive_name} would be extracted into Docker volume ${STORAGE_DOCKER_VOLUME}"
    return
  fi
  docker run --rm \
    -v "${STORAGE_DOCKER_VOLUME}:/data" \
    -v "${archive_dir}:/backup:ro" \
    alpine:3.20 \
    sh -c "find /data -mindepth 1 -maxdepth 1 -exec rm -rf -- {} +; tar -xzf '/backup/${archive_name}' -C /data" >>"$log_file" 2>&1
}

verify_database_after_restore_host() {
  command -v psql >/dev/null 2>&1 || fail "psql not found. Install PostgreSQL client tools or use BACKUP_MODE=docker."
  PGPASSWORD="$DB_PASSWORD" psql --host="$DB_HOST" --port="$DB_PORT" --username="$DB_USER" --dbname="$DB_NAME" --tuples-only --no-align \
    --command "select count(*) from information_schema.tables where table_schema = 'public';" >>"$log_file" 2>&1 || fail "Database verification failed."
  PGPASSWORD="$DB_PASSWORD" psql --host="$DB_HOST" --port="$DB_PORT" --username="$DB_USER" --dbname="$DB_NAME" --tuples-only --no-align \
    --command "select count(*) from information_schema.tables where table_schema = 'public' and table_name = '__EFMigrationsHistory';" >>"$log_file" 2>&1 || fail "Migration history verification failed."
}

verify_database_after_restore_docker() {
  docker exec -e PGPASSWORD="$DB_PASSWORD" "$POSTGRES_CONTAINER" psql --host="${DB_HOST:-localhost}" --port="$DB_PORT" --username="$DB_USER" --dbname="$DB_NAME" --tuples-only --no-align \
    --command "select count(*) from information_schema.tables where table_schema = 'public';" >>"$log_file" 2>&1 || fail "Database verification failed."
  docker exec -e PGPASSWORD="$DB_PASSWORD" "$POSTGRES_CONTAINER" psql --host="${DB_HOST:-localhost}" --port="$DB_PORT" --username="$DB_USER" --dbname="$DB_NAME" --tuples-only --no-align \
    --command "select count(*) from information_schema.tables where table_schema = 'public' and table_name = '__EFMigrationsHistory';" >>"$log_file" 2>&1 || fail "Migration history verification failed."
}

main() {
  if [ "$LIST_ONLY" = "true" ]; then
    list_backups
    exit 0
  fi

  [ -n "$DB_NAME" ] || fail "DB_NAME is required"
  [ -n "$DB_USER" ] || fail "DB_USER is required"

  DB_DUMP_PATH="${DB_DUMP_PATH:-$(latest_db_dump)}"
  assert_readable_non_empty_file "$DB_DUMP_PATH"

  local db_ts db_size storage_size
  db_ts="$(timestamp_from_db_dump "$DB_DUMP_PATH")"
  if [ -z "${STORAGE_ARCHIVE_PATH:-}" ] && [ -n "$db_ts" ]; then
    STORAGE_ARCHIVE_PATH="$(matching_storage_archive "$db_ts" || true)"
  fi

  db_size="$(wc -c <"$DB_DUMP_PATH" | tr -d ' ')"
  log "Selected database backup: $(basename "$DB_DUMP_PATH") ($(human_size "$db_size"))"
  if [ -n "${STORAGE_ARCHIVE_PATH:-}" ]; then
    storage_size="$(wc -c <"$STORAGE_ARCHIVE_PATH" | tr -d ' ')"
    log "Selected storage backup: $(basename "$STORAGE_ARCHIVE_PATH") ($(human_size "$storage_size"))"
  else
    log "WARNING: No matching storage backup found for timestamp ${db_ts:-unknown}"
  fi

  confirm_restore
  log "Starting HOP restore"
  log "Mode=${BACKUP_MODE}; DB=${DB_HOST}:${DB_PORT}/${DB_NAME}; BackupRoot=${BACKUP_ROOT}; StoragePath=${STORAGE_PATH}; RestoreDatabase=${RESTORE_DATABASE}; RestoreStorage=${RESTORE_STORAGE}; DryRun=${DRY_RUN}; BackupId=${BACKUP_ID:-none}"

  case "$BACKUP_MODE" in
    host)
      if [ "$RESTORE_DATABASE" = "true" ]; then
        restore_database_host
        verify_database_after_restore_host
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
        verify_database_after_restore_docker
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
}

main
