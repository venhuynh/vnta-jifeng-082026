#!/usr/bin/env bash
set -euo pipefail

DEPLOY_ROOT="${1:-/opt/vnta}"
DEPLOY_USER="${2:-$USER}"

sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg

need_docker_install="false"
if ! command -v docker >/dev/null 2>&1; then
  need_docker_install="true"
elif ! docker compose version >/dev/null 2>&1; then
  need_docker_install="true"
fi

if [[ "$need_docker_install" = "true" ]]; then
  sudo install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  sudo chmod a+r /etc/apt/keyrings/docker.gpg

  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
    $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
    sudo tee /etc/apt/sources.list.d/docker.list >/dev/null

  sudo apt-get update
  sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
fi

sudo mkdir -p "$DEPLOY_ROOT/releases"
sudo mkdir -p "$DEPLOY_ROOT/shared/env"
sudo mkdir -p "$DEPLOY_ROOT/shared/logs/hrm"
sudo mkdir -p "$DEPLOY_ROOT/shared/logs/adms"
sudo mkdir -p "$DEPLOY_ROOT/shared/logs/adms-raw"
sudo mkdir -p "$DEPLOY_ROOT/shared/backups"
sudo chown -R "$DEPLOY_USER:$DEPLOY_USER" "$DEPLOY_ROOT"

if id -nG "$DEPLOY_USER" | tr ' ' '\n' | grep -qx docker; then
  :
else
  sudo usermod -aG docker "$DEPLOY_USER"
fi

docker --version
docker compose version

echo ""
echo "Bootstrap xong. Nếu vừa thêm user vào group docker, hãy đăng xuất SSH rồi vào lại."
