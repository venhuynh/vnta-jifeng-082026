#!/usr/bin/env bash
set -euo pipefail

TARGET_ROOT="${1:-/opt/vnta}"

echo "Kiểm tra source code trên: $TARGET_ROOT"

matches="$(find "$TARGET_ROOT" \
  \( -name ".git" -o -name "*.sln" -o -name "*.slnx" -o -name "*.csproj" -o -name "*.cs" -o -name "*.razor" -o -path "*/src/*" \) \
  -print)"

if [[ -n "$matches" ]]; then
  echo "Phát hiện dấu vết source không mong muốn:" >&2
  printf '%s\n' "$matches" >&2
  exit 1
fi

echo "OK: không tìm thấy source code / git metadata trong $TARGET_ROOT"
