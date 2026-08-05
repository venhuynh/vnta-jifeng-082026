#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${1:-/opt/vnta/shared/env/.env.production}"
HRM_IMAGE="${2:?Cần truyền HRM image}"
ADMS_IMAGE="${3:-}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Không tìm thấy env file: $ENV_FILE" >&2
  exit 1
fi

validate_image() {
  local image="$1"
  if [[ ! "$image" =~ ^[A-Za-z0-9._/-]+:[A-Za-z0-9._-]+$ ]]; then
    echo "Image tag không hợp lệ: $image" >&2
    exit 1
  fi
}

set_env_value() {
  local key="$1"
  local value="$2"

  if grep -q "^${key}=" "$ENV_FILE"; then
    sed -i "s|^${key}=.*$|${key}=${value}|" "$ENV_FILE"
  else
    printf '\n%s=%s\n' "$key" "$value" >> "$ENV_FILE"
  fi
}

validate_image "$HRM_IMAGE"
set_env_value "HRM_IMAGE" "$HRM_IMAGE"

if [[ -n "$ADMS_IMAGE" ]]; then
  validate_image "$ADMS_IMAGE"
  set_env_value "ADMS_IMAGE" "$ADMS_IMAGE"
fi

echo "Đã cập nhật image tag trong $ENV_FILE"
