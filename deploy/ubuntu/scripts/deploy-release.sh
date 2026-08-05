#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
RELEASE_DIR="$(cd -- "$SCRIPT_DIR/.." && pwd)"
ENV_FILE="${1:-/opt/vnta/shared/env/.env.production}"
DEPLOYMENT_MODE="${2:-HrmAndGateway}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Không tìm thấy env file: $ENV_FILE" >&2
  exit 1
fi

set -a
. "$ENV_FILE"
set +a

DEPLOY_ROOT="${DEPLOY_ROOT:-/opt/vnta}"
CURRENT_LINK="$DEPLOY_ROOT/current"

mkdir -p "${HRM_LOG_DIR:?set HRM_LOG_DIR}"
mkdir -p "${ADMS_LOG_DIR:?set ADMS_LOG_DIR}"
mkdir -p "${ADMS_RAW_LOG_DIR:?set ADMS_RAW_LOG_DIR}"
mkdir -p "${BACKUP_DIR:-$DEPLOY_ROOT/shared/backups}"

if [[ -e "$CURRENT_LINK" && ! -L "$CURRENT_LINK" ]]; then
  echo "Từ chối ghi đè đường dẫn không phải symlink: $CURRENT_LINK" >&2
  exit 1
fi

case "$DEPLOYMENT_MODE" in
  HrmOnly)
    DEPLOY_SERVICES=(hrm-web)
    ;;
  HrmAndGateway)
    DEPLOY_SERVICES=(hrm-web adms-gateway)
    ;;
  *)
    echo "Deployment mode không hợp lệ: $DEPLOYMENT_MODE. Dùng HrmOnly hoặc HrmAndGateway." >&2
    exit 1
    ;;
esac

if compgen -G "$RELEASE_DIR/images/*.tar" >/dev/null; then
  for image_tar in "$RELEASE_DIR"/images/*.tar; do
    echo "Đang nạp image: $image_tar"
    docker load -i "$image_tar"
  done
fi

ln -sfn "$RELEASE_DIR" "$CURRENT_LINK"

if [[ "$DEPLOYMENT_MODE" == "HrmOnly" ]]; then
  docker compose \
    -f "$RELEASE_DIR/docker-compose.production.yml" \
    --env-file "$ENV_FILE" \
    up -d --no-deps hrm-web
else
  docker compose \
    -f "$RELEASE_DIR/docker-compose.production.yml" \
    --env-file "$ENV_FILE" \
    up -d --remove-orphans "${DEPLOY_SERVICES[@]}"
fi

docker compose \
  -f "$RELEASE_DIR/docker-compose.production.yml" \
  --env-file "$ENV_FILE" \
  ps

echo ""
echo "Deploy xong. HRM origin dự kiến: ${HRM_PUBLIC_ORIGIN:-unset}"
echo "ADMS listener port dự kiến: ${ADMS_LISTENER_PORT:-unset}"
echo "Deployment mode: $DEPLOYMENT_MODE"
