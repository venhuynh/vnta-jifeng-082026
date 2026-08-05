# Deploy Ubuntu Tự Động Từ PowerShell

Tài liệu này dùng cho Ubuntu test đã được bootstrap theo `ubuntu-docker-deployment.md`, đã có Docker, firewall, certificate và `/opt/vnta/shared/env/.env.production`.

Script tự động thực hiện:

1. kiểm tra worktree Git sạch và Docker Desktop sẵn sàng;
2. build image cần thiết, tạo release artifact và SHA-256 manifest;
3. upload artifact lên Ubuntu qua SSH/SCP;
4. cập nhật image tag trong `.env.production` trên server mà không thay đổi secret;
5. backup PostgreSQL, mặc định không được bỏ qua;
6. deploy mode đã chọn và kiểm tra server không có source/Git metadata.

Script không chạy EF migration. Database test hiện được vận hành độc lập và đã được xác nhận không cần migration trong quy trình này.

## Bắt đầu từ PowerShell trên Windows

Thực hiện các lệnh dưới đây trên PC Windows đang chứa source code và Docker Desktop. Không chạy script này trên Ubuntu.

1. Mở **Windows PowerShell** hoặc **PowerShell**:
   - Nhấn phím `Windows`, gõ `PowerShell`, rồi chọn ứng dụng tương ứng.
   - Không cần mở quyền Administrator, trừ khi chính sách máy của bạn yêu cầu.
2. Chuyển đến thư mục gốc của repository. Với workspace hiện tại, chạy:

```powershell
Set-Location <repository-root>
```

   Nếu repository nằm ở nơi khác, thay đường dẫn trên bằng thư mục chứa `deploy`, `src` và file `README.md`. Có thể kiểm tra thư mục hiện tại bằng:

```powershell
Get-Location
```

3. Kiểm tra bạn đang ở đúng repository, worktree sạch và Docker Desktop đã sẵn sàng:

```powershell
Test-Path .\deploy\ubuntu\scripts\publish-ubuntu-release.ps1
git status --short
docker info
```

   - Lệnh `Test-Path` phải trả về `True`.
   - `git status --short` không được in dòng nào. Nếu có thay đổi, hãy commit hoặc stash trước khi deploy.
   - `docker info` phải hoàn tất thành công. Nếu báo không kết nối được Docker daemon, hãy mở Docker Desktop và chờ Docker khởi động xong.

4. Chạy một trong các lệnh deploy ở phần bên dưới. Khi chưa dùng SSH key, PowerShell có thể yêu cầu nhập mật khẩu SSH nhiều lần cho các lượt kiểm tra, upload và deploy. Không dán mật khẩu, secret HMAC hoặc chuỗi kết nối database vào terminal nếu không thực sự cần thiết.

## Điều kiện trước khi chạy

- PowerShell Windows và Docker Desktop đang chạy.
- OpenSSH client (`ssh`, `scp`) dùng được từ PowerShell.
- Nhánh hiện tại đã commit/push; worktree không có thay đổi.
- Ubuntu đã có `.env.production`, PFX certificate và HMAC secret hợp lệ.
- Nếu chưa cấu hình SSH key, script sẽ hỏi mật khẩu SSH cho các lượt `ssh`/`scp`. Nên cài SSH key để không cần nhập lại mật khẩu.

Không lưu DB password, HMAC secret, PFX password hoặc `.env.production` trong repository.

## 1. Chỉ deploy HRM

Chế độ này chỉ build/nạp/restart service `hrm-web`; `adms-gateway` tiếp tục chạy image hiện có và không bị restart. Backup database vẫn chạy.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\deploy\ubuntu\scripts\publish-ubuntu-release.ps1 -DeploymentMode HrmOnly
```

## 2. Deploy HRM và ADMS Gateway

Chế độ này build/nạp cả hai image rồi restart cả `hrm-web` và `adms-gateway`.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\deploy\ubuntu\scripts\publish-ubuntu-release.ps1 -DeploymentMode HrmAndGateway
```

Mặc định script tự tạo release version theo thời điểm chạy, ví dụ `2026.07.21-093015`. Muốn dùng version cố định:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\deploy\ubuntu\scripts\publish-ubuntu-release.ps1 `
  -DeploymentMode HrmAndGateway `
  -ReleaseVersion 2026.07.21-test.2
```

## Tùy chọn vận hành

- `-ServerHost`, `-SshUser`, `-SshPort`, `-DeployRoot`: đổi thông tin kết nối/server.
- `-ImageNamespace`: đổi namespace image, mặc định `vnta`.
- `-SkipDatabaseBackup`: chỉ dùng khi người vận hành đã tạo và xác nhận một backup khác cho đúng thời điểm deploy.

Ví dụ dùng server khác và SSH port khác:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\deploy\ubuntu\scripts\publish-ubuntu-release.ps1 `
  -DeploymentMode HrmOnly `
  -ServerHost 192.168.1.218 `
  -SshPort 22
```

## Sau deploy

- HRM test: `https://192.168.1.218:8443`.
- ADMS listener: `192.168.1.218:8080`.
- Với self-signed certificate test, browser có thể hỏi/chặn certificate. Không chọn certificate client trên browser; certificate gateway chỉ dành cho container ADMS.
- Xem trạng thái/log trên Ubuntu:

```bash
cd /opt/vnta/current
docker compose --env-file /opt/vnta/shared/env/.env.production -f docker-compose.production.yml ps
docker compose --env-file /opt/vnta/shared/env/.env.production -f docker-compose.production.yml logs --tail 200
```

## Rollback

Rollback vẫn dùng script trên Ubuntu. Trước khi rollback, xác định release cũ và giữ `.env.production` trỏ tới image tag tương ứng:

```bash
/opt/vnta/current/scripts/rollback-release.sh <release-name> /opt/vnta/shared/env/.env.production
```
