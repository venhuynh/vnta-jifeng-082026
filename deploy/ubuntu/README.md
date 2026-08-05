# Tài Nguyên Deploy Docker Cho Ubuntu

Thư mục này chứa bộ file mẫu để triển khai theo hướng:

- build image trên PC Windows đang làm release
- `docker save` ra `.tar`
- upload artifact lên Ubuntu
- `docker load` và `docker compose up -d`
- không đưa source code lên server

## File Chính

- `hrm-web.Dockerfile`
- `adms-gateway.Dockerfile`
- `docker-compose.production.yml`
- `.env.production.example`
- `scripts/package-release.ps1`
- `scripts/run-hrm-db-migration.ps1`
- `sql/audit-runtime-grants.sql.example`
- `scripts/bootstrap-ubuntu.sh`
- `scripts/deploy-release.sh`
- `scripts/backup-db.sh`
- `scripts/rollback-release.sh`
- `scripts/verify-no-source.sh`

## Nhịp Dùng Đề Xuất

1. Chạy migration thủ công từ PC bằng `scripts/run-hrm-db-migration.ps1`.
2. Đóng gói release bằng `scripts/package-release.ps1`.
3. Upload thư mục release lên `/opt/vnta/releases/<release>`.
4. Tạo `/opt/vnta/shared/env/.env.production`.
5. Chạy `scripts/backup-db.sh`.
6. Chạy `scripts/deploy-release.sh`.
7. Chạy `scripts/verify-no-source.sh`.

## Deploy Tự Động Từ Windows PowerShell

Sau khi Ubuntu đã được chuẩn bị một lần, dùng `scripts/publish-ubuntu-release.ps1` để build, upload, backup và deploy trong một lệnh. Script hỗ trợ hai mode: `HrmOnly` và `HrmAndGateway`.

Hướng dẫn đầy đủ: `doc/setup/automated-ubuntu-release.md`.

Runbook đầy đủ nằm ở `doc/setup/ubuntu-docker-deployment.md`.
