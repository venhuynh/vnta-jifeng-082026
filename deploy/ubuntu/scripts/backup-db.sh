#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${1:-/opt/vnta/shared/env/.env.production}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Không tìm thấy env file: $ENV_FILE" >&2
  exit 1
fi

set -a
. "$ENV_FILE"
set +a

BACKUP_DIR="${BACKUP_DIR:-/opt/vnta/shared/backups}"
TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_NAME="${COMPOSE_PROJECT_NAME:-vnta}-${TIMESTAMP}.dump"

mkdir -p "$BACKUP_DIR"

docker run --rm \
  -e PGPASSWORD="${DATABASE_PASSWORD:?set DATABASE_PASSWORD}" \
  -v "$BACKUP_DIR:/backup" \
  postgres:16-alpine \
  pg_dump \
    --format=custom \
    --host "${DATABASE_HOST:?set DATABASE_HOST}" \
    --port "${DATABASE_PORT:?set DATABASE_PORT}" \
    --username "${DATABASE_USERNAME:?set DATABASE_USERNAME}" \
    --dbname "${DATABASE_NAME:?set DATABASE_NAME}" \
    --file "/backup/$BACKUP_NAME"

echo "Backup đã tạo: $BACKUP_DIR/$BACKUP_NAME"
