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
- `scripts/reset-hrm-business-data.sh`
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

## Reset dữ liệu nghiệp vụ

Khi cần làm sạch database để nhập lại dữ liệu, dùng
`scripts/reset-hrm-business-data.sh`. Script bắt buộc tạo và kiểm tra backup
trước khi reset, xóa các dòng dữ liệu ứng dụng trong schema `public` và
`audit`, đồng thời giữ nguyên ASP.NET Core Identity và
`__EFMigrationsHistory`.

Script chỉ chạy khi `DATABASE_NAME=jifeng_hrm`, để tránh reset nhầm database khác.

Trước khi chạy, dừng cả `hrm-web` và `adms-gateway` để gateway không ghi lại
dữ liệu trong lúc reset. Chạy bằng DB owner hoặc một role có quyền `ALTER`,
`TRUNCATE` trên các bảng HRM/ADMS:

```bash
./scripts/reset-hrm-business-data.sh /opt/vnta/shared/env/.env.production --confirm-reset
```

Việc reset đặt `AspNetUsers.EmployeeId` về `NULL` để có thể xóa nhân viên mà
vẫn giữ tài khoản, mật khẩu và phân quyền. Sau khi nhập dữ liệu nhân sự mới,
các tài khoản cũ cần được gắn lại với nhân viên tương ứng.

## Deploy Tự Động Từ Windows PowerShell

Sau khi Ubuntu đã được chuẩn bị một lần, dùng `scripts/publish-ubuntu-release.ps1` để build, upload, backup và deploy trong một lệnh. Script hỗ trợ hai mode: `HrmOnly` và `HrmAndGateway`.

Hướng dẫn đầy đủ: `doc/setup/automated-ubuntu-release.md`.

Runbook đầy đủ nằm ở `doc/setup/ubuntu-docker-deployment.md`.
