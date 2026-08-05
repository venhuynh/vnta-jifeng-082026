#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Cách dùng: $0 <release-name-or-absolute-path> [env-file]" >&2
  exit 1
fi

TARGET_INPUT="$1"
ENV_FILE="${2:-/opt/vnta/shared/env/.env.production}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Không tìm thấy env file: $ENV_FILE" >&2
  exit 1
fi

set -a
. "$ENV_FILE"
set +a

DEPLOY_ROOT="${DEPLOY_ROOT:-/opt/vnta}"
CURRENT_LINK="$DEPLOY_ROOT/current"

if [[ "$TARGET_INPUT" = /* ]]; then
  TARGET_RELEASE_DIR="$TARGET_INPUT"
else
  TARGET_RELEASE_DIR="$DEPLOY_ROOT/releases/$TARGET_INPUT"
fi

if [[ ! -d "$TARGET_RELEASE_DIR" ]]; then
  echo "Không tìm thấy release: $TARGET_RELEASE_DIR" >&2
  exit 1
fi

if [[ -e "$CURRENT_LINK" && ! -L "$CURRENT_LINK" ]]; then
  echo "Từ chối ghi đè đường dẫn không phải symlink: $CURRENT_LINK" >&2
  exit 1
fi

if compgen -G "$TARGET_RELEASE_DIR/images/*.tar" >/dev/null; then
  for image_tar in "$TARGET_RELEASE_DIR"/images/*.tar; do
    echo "Đang nạp image: $image_tar"
    docker load -i "$image_tar"
  done
fi

ln -sfn "$TARGET_RELEASE_DIR" "$CURRENT_LINK"

docker compose \
  -f "$TARGET_RELEASE_DIR/docker-compose.production.yml" \
  --env-file "$ENV_FILE" \
  up -d --remove-orphans

docker compose \
  -f "$TARGET_RELEASE_DIR/docker-compose.production.yml" \
  --env-file "$ENV_FILE" \
  ps

echo "Rollback xong về release: $TARGET_RELEASE_DIR"
