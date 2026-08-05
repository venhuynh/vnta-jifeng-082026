# Triển Khai Ubuntu Bằng Docker Image-Only

Tài liệu này chốt quy trình deploy image-only cho JIFENG HRM theo đúng mục tiêu:

- không đưa source code lên server Ubuntu
- không build source trên server
- build image Docker trên PC Windows đang làm release
- export image thành file `.tar`
- upload artifact lên server
- runtime secret nằm trong `.env.production` trên server
- migration database chạy thủ công trước release

## Phạm Vi Và Giả Định Đã Chốt

- Thành phần deploy: `Vnta.Hrm.Web` + `zkteco-adms-gateway`
- PostgreSQL production hiện vẫn nằm ở máy DB nội bộ, không chạy cùng Docker stack trên Ubuntu
- Nghĩa là database vẫn nằm trong operational scope của release này, nhưng được quản lý theo hướng external DB: migration, backup, rollback dữ liệu tách riêng khỏi Docker Compose
- Luồng SSH, `scp`, truy cập HRM và kết nối ADMS trong tài liệu này đều được hiểu là đi trong cùng LAN nội bộ
- HRM public trực tiếp bằng HTTPS; chứng thư PFX được mount vào container, không đặt sau Nginx trong runbook này
- ADMS chỉ public `listener port`; control plane port giữ nội bộ trong Docker network
- Ubuntu mục tiêu được giả định là `Ubuntu 24.04 LTS` (bản phát hành tháng 04/2024). Bạn đã ghi `24.4`, nên tài liệu này chuẩn hóa thành `24.04`

## Các File Đã Chuẩn Bị Trong Repo

- `deploy/ubuntu/hrm-web.Dockerfile`
- `deploy/ubuntu/adms-gateway.Dockerfile`
- `deploy/ubuntu/docker-compose.production.yml`
- `deploy/ubuntu/.env.production.example`
- `deploy/ubuntu/scripts/package-release.ps1`
- `deploy/ubuntu/scripts/run-hrm-db-migration.ps1`
- `deploy/ubuntu/scripts/bootstrap-ubuntu.sh`
- `deploy/ubuntu/scripts/deploy-release.sh`
- `deploy/ubuntu/scripts/backup-db.sh`
- `deploy/ubuntu/scripts/rollback-release.sh`
- `deploy/ubuntu/scripts/verify-no-source.sh`

## Thông Tin Server Đã Chốt

- Server IP: `192.168.1.218`
- SSH user: `vns`
- Server Ubuntu này nằm cùng mạng LAN với PC đang build release
- `192.168.1.218` là private LAN IP, không phải public IP

Tài liệu và file mẫu còn lại các placeholder sau. Trước khi deploy thật, thay bằng giá trị của bạn:

- `SSH_PORT`
- `DEPLOY_ROOT`
- `DATABASE_PASSWORD`
- tag image release, ví dụ `2026.07.08-01`

## Topology Đề Xuất

Do không dùng Nginx và không muốn ADMS listener trùng port với HRM, bộ port mẫu được chốt như sau:

- HRM web: host port `8443` -> container port `8443` (HTTPS)
- ADMS listener: host port `8080` -> container port `8080`
- ADMS control plane: container port `5005`, không public ra host

Khi đó:

- HRM truy cập bằng `https://192.168.1.218:8443`
- thiết bị ADMS gọi vào `192.168.1.218:8080`
- từ PC cùng LAN, có thể mở trực tiếp `https://192.168.1.218:8443` để smoke test sau deploy

## Cấu Trúc Thư Mục Trên Server

```text
/opt/vnta/
  current -> /opt/vnta/releases/<release-name>
  releases/
    <release-name>/
      docker-compose.production.yml
      .env.production.example
      images/
        hrm-web.tar
        adms-gateway.tar
      scripts/
  shared/
    env/
      .env.production
    logs/
      hrm/
      adms/
      adms-raw/
    backups/
```

## 1. Chạy Migration Thủ Công Trên PC Release

Mục này chạy trên chính PC Windows đang giữ source. Không chạy migration từ server.

### 1.1. Vì Sao Chỉ Chạy Migration Bên PC

- Bảo vệ source: server không cần repo git
- Giữ rõ boundary release: schema được cập nhật trước khi image mới lên server
- Giảm rủi ro app tự ý migrate lúc startup

### 1.2. Script Migration

Đã có script:

- `deploy/ubuntu/scripts/run-hrm-db-migration.ps1`

Ví dụ:

```powershell
pwsh .\deploy\ubuntu\scripts\run-hrm-db-migration.ps1 `
  -ConnectionString "Host=192.168.1.199;Port=5432;Database=jifeng_hrm;Username=postgres;Password=YOUR_PASSWORD;Timezone=Asia/Ho_Chi_Minh"
```

Lưu ý:

- Script này cập nhật schema qua `ApplicationDbContext`
- Các migration HRM hiện tại đã bao gồm baseline schema attendance/gateway trong `Vnta.Hrm.Infrastructure`
- Runbook này không bật auto-migrate trong container production

### 1.3. Điều kiện hoàn tất migration

`run-hrm-db-migration.ps1` là bước bắt buộc trước khi đóng gói release. Script tự động:

1. chạy `dotnet ef database update` để áp dụng toàn bộ migration chưa có trong database;
2. chạy `dotnet ef migrations has-pending-model-changes` để chặn release nếu model EF còn thay đổi chưa được đưa vào migration.

Chỉ tiếp tục sang bước đóng gói khi lệnh kết thúc thành công và có dòng:

```text
No changes have been made to the model since the last migration.
```

Nếu database đã đầy đủ, kết quả `database update` có thể là:

```text
No migrations were applied. The database is already up to date.
```

Đây là trạng thái hợp lệ: không có migration chờ áp dụng. Nếu kiểm tra model thất bại, tạo và rà soát migration trong source trước; không bỏ qua lỗi đó để deploy, vì ứng dụng sẽ phát sinh `PendingModelChangesWarning` khi khởi động.

## 2. Đóng Gói Release Trên PC Windows

### 2.1. Build Image Và Xuất Artifact

Đã có script:

- `deploy/ubuntu/scripts/package-release.ps1`

Ví dụ:

```powershell
pwsh .\deploy\ubuntu\scripts\package-release.ps1 -ReleaseVersion 2026.07.08-01
```

Script sẽ:

1. Build image `vnta/hrm-web:<release>`
2. Build image `vnta/adms-gateway:<release>`
3. `docker save` thành:
   - `images/hrm-web.tar`
   - `images/adms-gateway.tar`
4. Copy sang release package:
   - `docker-compose.production.yml`
   - `.env.production.example`
   - `scripts/*.sh`
5. Tạo:
   - `release-manifest.txt`
   - `sha256sums.txt`

### 2.2. Thư Mục Artifact Sau Khi Đóng Gói

Mặc định artifact sẽ ra:

```text
.artifacts/releases/ubuntu-docker-2026.07.08-01/
```

## 3. Chuẩn Bị Ubuntu Server

### 3.1. SSH Vào Server

Thông tin đã chốt:

- host: `192.168.1.218`
- user: `vns`
- port: nếu chưa đổi thì dùng mặc định `22`
- kết nối trực tiếp trong LAN, không cần jump host hay NAT public

```powershell
ssh vns@192.168.1.218
```

Nếu server đang dùng port SSH khác `22`:

```powershell
ssh -p SSH_PORT vns@192.168.1.218
```

Mật khẩu SSH hiện tại nên được giữ ngoài repo. Sau khi đăng nhập lần đầu, ưu tiên cài SSH key và tắt dần password auth nếu hạ tầng cho phép.

### 3.2. Cài Docker Và Tạo Thư Mục Deploy

Trên Ubuntu, sau khi upload repo artifact hoặc copy script qua server, chạy:

```bash
chmod +x bootstrap-ubuntu.sh
./bootstrap-ubuntu.sh /opt/vnta vns
```

Nếu muốn dùng đường dẫn khác, đổi `/opt/vnta`.

Script này sẽ:

- cài Docker Engine + Compose plugin
- tạo các thư mục `releases`, `shared/env`, `shared/logs`, `shared/backups`
- thêm user deploy vào group `docker`

Nếu vừa thêm group `docker`, đăng xuất SSH và vào lại trước khi deploy.

### 3.3. Firewall Tối Thiểu

Do không dùng Nginx nhưng HRM dùng HTTPS trực tiếp, chỉ cần mở:

- SSH port thật của bạn
- HRM HTTPS port: `8443`
- ADMS listener port: `8080`
- ưu tiên chỉ mở trong LAN nội bộ, không route các cổng này ra internet nếu không thực sự cần

Ví dụ nếu SSH vẫn là `22`:

```bash
sudo ufw allow 22/tcp
sudo ufw allow 8443/tcp
sudo ufw allow 8080/tcp
sudo ufw enable
sudo ufw status
```

Không cần mở `5005/tcp` nếu bạn giữ ADMS control plane nội bộ.

## 4. Tạo `.env.production` Trên Server

Copy file mẫu:

```bash
cp /opt/vnta/releases/<release-name>/.env.production.example /opt/vnta/shared/env/.env.production
nano /opt/vnta/shared/env/.env.production
```

Schema đầy đủ nằm trong `.env.production.example`. Khi tạo môi trường Jifeng, thay các
giá trị database trong file đã copy theo baseline dưới đây và thay toàn bộ placeholder
`CHANGE_ME`; không sao chép lại block HTTP hoặc database legacy từ các release cũ.

```dotenv
COMPOSE_PROJECT_NAME=vnta-hrm-2026
DEPLOY_ROOT=/opt/vnta
TZ=Asia/Ho_Chi_Minh

HRM_IMAGE=vnta/hrm-web:2026.07.08-01
ADMS_IMAGE=vnta/adms-gateway:2026.07.08-01

HRM_HTTPS_CONTAINER_PORT=8443
HRM_HTTPS_PORT=8443
HRM_PUBLIC_ORIGIN=https://192.168.1.218:8443
HRM_CERT_DIR=/opt/vnta/shared/certificates/hrm
HRM_TLS_CERT_PASSWORD=CHANGE_ME

ADMS_LISTENER_PORT=8080
ADMS_CONTROL_PLANE_PORT=5005
ADMS_LOG_DIR=/opt/vnta/shared/logs/adms
ADMS_RAW_LOG_DIR=/opt/vnta/shared/logs/adms-raw
HRM_LOG_DIR=/opt/vnta/shared/logs/hrm
BACKUP_DIR=/opt/vnta/shared/backups

DATABASE_HOST=192.168.1.199
DATABASE_PORT=5432
DATABASE_NAME=jifeng_hrm
DATABASE_USERNAME=postgres
DATABASE_PASSWORD=YOUR_PASSWORD
DATABASE_TIMEZONE=Asia/Ho_Chi_Minh

HRM_DB_CONNECTION=Host=192.168.1.199;Port=5432;Database=jifeng_hrm;Username=postgres;Password=YOUR_PASSWORD;Timezone=Asia/Ho_Chi_Minh
ADMS_DB_CONNECTION=Host=192.168.1.199;Port=5432;Database=jifeng_hrm;Username=postgres;Password=YOUR_PASSWORD;Timezone=Asia/Ho_Chi_Minh

ADMS_CORE_API_ENABLED=true
ADMS_CORE_API_BASE_URL=https://hrm-web:8443
GATEWAY_CERT_DIR=/opt/vnta/shared/certificates/gateway
GATEWAY_CLIENT_CERT_PASSWORD=CHANGE_ME
GATEWAY_CLIENT_CERT_SHA256_THUMBPRINT=CHANGE_ME
HRM_SERVER_CERT_SHA256_THUMBPRINT=CHANGE_ME
GATEWAY_HMAC_KEY_ID=gateway-2026-01
GATEWAY_HMAC_SECRET=CHANGE_ME
```

Giải thích ngắn:

- `HRM_PUBLIC_ORIGIN` được đưa vào CORS/origin config cho gateway
- `ADMS_CORE_API_BASE_URL=https://hrm-web:8443` giúp gateway gọi ngược vào HRM trong cùng Docker network
- `5005` không public, nhưng vẫn tồn tại nội bộ để gateway chạy API control/hub của nó
- `DATABASE_NAME`, `HRM_DB_CONNECTION` và `ADMS_DB_CONNECTION` phải cùng trỏ đến `jifeng_hrm`; không deploy mode `HrmOnly` trong lần chuyển database vì gateway có thể vẫn giữ connection string cũ

## 5. Upload Release Package Lên Server

Từ PC Windows, ví dụ:

```powershell
scp -r .\.artifacts\releases\ubuntu-docker-2026.07.08-01 vns@192.168.1.218:/opt/vnta/releases/
```

Do PC và server cùng LAN, lệnh `scp` này đi thẳng trong mạng nội bộ.

Nếu server dùng SSH port khác `22`:

```powershell
scp -P SSH_PORT -r .\.artifacts\releases\ubuntu-docker-2026.07.08-01 vns@192.168.1.218:/opt/vnta/releases/
```

Sau khi upload xong, trên server thư mục sẽ là:

```text
/opt/vnta/releases/ubuntu-docker-2026.07.08-01/
```

Cấp quyền execute cho script:

```bash
cd /opt/vnta/releases/ubuntu-docker-2026.07.08-01
chmod +x scripts/*.sh
```

## 6. Backup Trước Khi Deploy

Trên server:

```bash
cd /opt/vnta/releases/ubuntu-docker-2026.07.08-01
./scripts/backup-db.sh /opt/vnta/shared/env/.env.production
```

Script sẽ dùng image `postgres:16-alpine` để chạy `pg_dump`, không cần cài thêm `psql` trên host.

File backup mặc định sẽ nằm trong:

```text
/opt/vnta/shared/backups/
```

## 7. Deploy Release

Trên server:

```bash
cd /opt/vnta/releases/ubuntu-docker-2026.07.08-01
./scripts/deploy-release.sh /opt/vnta/shared/env/.env.production
```

Script sẽ:

1. `docker load` các file `.tar`
2. cập nhật symlink `current`
3. `docker compose up -d --remove-orphans`

## 8. Kiểm Tra Sau Deploy

### 8.1. Trạng Thái Container

```bash
cd /opt/vnta/current
docker compose --env-file /opt/vnta/shared/env/.env.production -f docker-compose.production.yml ps
```

### 8.2. Log

```bash
cd /opt/vnta/current
docker compose --env-file /opt/vnta/shared/env/.env.production -f docker-compose.production.yml logs --tail 200
```

### 8.3. Truy Cập Dịch Vụ

- HRM: `https://192.168.1.218:8443`
- ADMS listener: `192.168.1.218:8080`
- từ chính PC đang build release, đây là hai địa chỉ ưu tiên để kiểm tra ngay sau deploy

### 8.4. Kiểm Tra Bảo Vệ Source

```bash
./scripts/verify-no-source.sh /opt/vnta
```

Script phải báo:

```text
OK: không tìm thấy source code / git metadata ...
```

## 9. Rollback

Giả sử release cũ an toàn là `ubuntu-docker-2026.07.07-02`.

```bash
/opt/vnta/current/scripts/rollback-release.sh ubuntu-docker-2026.07.07-02 /opt/vnta/shared/env/.env.production
```

Script rollback sẽ:

1. load lại image `.tar` trong release mục tiêu nếu cần
2. đổi symlink `current`
3. `docker compose up -d --remove-orphans` trên release cũ

## 10. Restore Database Khi Cần

Runbook này đã có backup, nhưng restore nên được chạy có kiểm soát và chỉ khi đã xác nhận rollback image là chưa đủ.

Ví dụ restore từ một file dump:

```bash
docker run --rm \
  -e PGPASSWORD="YOUR_PASSWORD" \
  -v /opt/vnta/shared/backups:/backup \
  postgres:16-alpine \
  pg_restore \
    --clean \
    --if-exists \
    --host 192.168.1.199 \
    --port 5432 \
    --username postgres \
    --dbname jifeng_hrm \
    /backup/<dump-file>.dump
```

Cần thực hiện restore trong maintenance window, vì lệnh này có thể ghi đè schema/data hiện tại.

## 11. Checklist Nguồn Phát Hành

Trước khi go-live, check lại:

1. Migration production đã chạy xong từ PC
2. `.env.production` trên server đã đổi password/IP/port thật
3. `HRM_IMAGE` và `ADMS_IMAGE` trong env khớp với release đang upload
4. Đã backup DB trước deploy
5. Server không có:
   - `.git/`
   - `src/`
   - `*.sln`
   - `*.csproj`
   - `*.cs`
   - `*.razor`
6. `5005` không public nếu không thực sự cần

## 12. Ghi Chú Vận Hành

- `docker-compose.production.yml` được thiết kế cho external PostgreSQL
- Nếu sau này bạn muốn đưa PostgreSQL vào cùng server Ubuntu, nên tách thành một runbook riêng để tránh nhầm với production topology hiện tại
- HRM production hiện đang đi trực tiếp bằng HTTPS tại `8443`; nếu sau này đặt reverse proxy phía trước, chốt lại certificate, port và `HRM_PUBLIC_ORIGIN` cùng lúc
- Vì đây là private LAN deployment, runbook này không bao gồm port forwarding, public DNS, reverse proxy internet hay hardening theo mô hình public edge

## 13. Bắt Buộc Security Cho Gateway → HRM

Compose production hiện cấu hình HRM HTTPS tại `8443`. Trước deploy, tạo ngoài repository hai thư mục chỉ đọc bởi user deploy:

```text
/opt/vnta/shared/certificates/hrm/hrm-server.pfx
/opt/vnta/shared/certificates/gateway/gateway-client.pfx
```

Điền trong `/opt/vnta/shared/env/.env.production` các biến `HRM_TLS_CERT_PASSWORD`, `GATEWAY_CLIENT_CERT_PASSWORD`, `GATEWAY_HMAC_SECRET`, `GATEWAY_CLIENT_CERT_SHA256_THUMBPRINT` và `HRM_SERVER_CERT_SHA256_THUMBPRINT`; không truyền các giá trị này bằng command line hoặc ghi chúng vào artifact release.

Certificate server phải có SAN `hrm-web`, vì gateway gọi `https://hrm-web:8443` trong Docker network. Nếu nhân viên truy cập HRM trực tiếp, SAN cũng phải chứa DNS/IP public tương ứng. Gateway ký HMAC từng request và gửi client certificate; HRM chỉ chấp nhận certificate client có thumbprint đã cấu hình. Chi tiết canonical request, xoay key và kiểm tra sau rollout nằm tại `doc/project/security/gateway-inbound-contract.md`.

Sau khi copy secret/certificate, đặt quyền tối thiểu, ví dụ:

```bash
sudo chown -R vns:vns /opt/vnta/shared/certificates /opt/vnta/shared/env
sudo chmod 700 /opt/vnta/shared/certificates /opt/vnta/shared/env
sudo chmod 600 /opt/vnta/shared/env/.env.production
```

Không deploy nếu còn `CHANGE_ME`, không có certificate, hoặc HRM/gateway chưa xác minh được hostname/certificate.

## Tham Khảo Đã Dùng Để Tạo Bộ Này

- Docker Engine install on Ubuntu (official): https://docs.docker.com/engine/install/ubuntu/
