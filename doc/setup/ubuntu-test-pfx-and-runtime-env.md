# Tạo PFX Test Và Hoàn Tất Runtime Env Cho Ubuntu

Runbook này dùng để chuẩn bị certificate và `/opt/vnta/shared/env/.env.production` trước khi chạy `publish-ubuntu-release.ps1`. Nó áp dụng cho môi trường test hiện tại:

- HRM public: `https://hrm-test.vnta.online`
- Ubuntu chạy Docker: `192.168.1.218`
- PostgreSQL external, chạy trong Docker Desktop Windows: `192.168.1.199:5432`

Không lưu PFX, password PFX, HMAC secret hoặc chuỗi kết nối thật trong Git.

> Certificate self-signed trong runbook này chỉ dành cho test. Browser sẽ cảnh báo certificate không tin cậy. Production phải dùng certificate từ CA được tin cậy hoặc CA nội bộ đã được quản trị.

## Điều kiện trước

- Docker Engine và Compose đã chạy được bằng user Ubuntu `vns`.
- Đã có các thư mục `/opt/vnta/shared/env` và `/opt/vnta/shared/certificates` trên Ubuntu.
- Windows PowerShell có `New-SelfSignedCertificate` và `Export-PfxCertificate`.
- NAT/firewall đã chuyển public TCP `443` tới Ubuntu TCP `8443`; TCP `80` tới Ubuntu TCP `80` chỉ cần khi sau này dùng Let's Encrypt HTTP-01.

Compose hiện dùng `https://hrm-web:8443` cho Gateway gọi HRM nội bộ. Vì vậy certificate HRM test phải có hai DNS SAN: `hrm-test.vnta.online` và `hrm-web`.

## 1. Tạo hai PFX trên Windows

Mở PowerShell bằng user Windows đang giữ PFX, rồi tạo thư mục nằm ngoài repository:

```powershell
$certDir = "C:\Users\Admin\vnta-certs"
New-Item -ItemType Directory -Force $certDir | Out-Null
Set-Location $certDir
```

### 1.1. Certificate HTTPS của HRM

```powershell
$hrm = New-SelfSignedCertificate `
  -DnsName "hrm-test.vnta.online","hrm-web" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -FriendlyName "VNTA HRM Test Server" `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -HashAlgorithm SHA256 `
  -KeyExportPolicy Exportable `
  -NotAfter (Get-Date).AddYears(2) `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")

$hrmPassword = Read-Host "Nhap password cho hrm-server.pfx" -AsSecureString

Export-PfxCertificate `
  -Cert $hrm `
  -FilePath "$certDir\hrm-server.pfx" `
  -Password $hrmPassword
```

Khi PowerShell hỏi password, nhập password mới. Ký tự không hiển thị. Không dùng password làm nội dung lời nhắc của `Read-Host` và không ghi plaintext password vào terminal.

### 1.2. Certificate client của ADMS Gateway

```powershell
$gateway = New-SelfSignedCertificate `
  -DnsName "gateway-client" `
  -CertStoreLocation "Cert:\CurrentUser\My" `
  -FriendlyName "VNTA Gateway Client Test" `
  -KeyAlgorithm RSA `
  -KeyLength 2048 `
  -HashAlgorithm SHA256 `
  -KeyExportPolicy Exportable `
  -NotAfter (Get-Date).AddYears(2) `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2")

$gatewayPassword = Read-Host "Nhap password cho gateway-client.pfx" -AsSecureString

Export-PfxCertificate `
  -Cert $gateway `
  -FilePath "$certDir\gateway-client.pfx" `
  -Password $gatewayPassword
```

Kiểm tra chỉ tên file, không in password:

```powershell
Get-ChildItem $certDir
```

Kết quả phải có `hrm-server.pfx` và `gateway-client.pfx`. Lưu hai password trong password manager.

## 2. Upload PFX lên Ubuntu

Tạo thư mục và giới hạn quyền trên Ubuntu:

```bash
install -d -m 700 /opt/vnta/shared/certificates/hrm
install -d -m 700 /opt/vnta/shared/certificates/gateway
```

Từ PowerShell Windows:

```powershell
scp "$certDir\hrm-server.pfx" `
  vns@192.168.1.218:/opt/vnta/shared/certificates/hrm/

scp "$certDir\gateway-client.pfx" `
  vns@192.168.1.218:/opt/vnta/shared/certificates/gateway/
```

Trên Ubuntu:

```bash
chmod 600 /opt/vnta/shared/certificates/hrm/hrm-server.pfx
chmod 600 /opt/vnta/shared/certificates/gateway/gateway-client.pfx
```

## 3. Lấy SHA-256 thumbprint

Các lệnh sau hỏi password PFX, sau đó trả về SHA-256 của raw certificate. Chỉ dùng hash trả về; không dùng hash `e3b0c442...` vì nó cho biết lệnh đã đọc dữ liệu rỗng do sai password hoặc thiếu file.

```bash
openssl pkcs12 \
  -in /opt/vnta/shared/certificates/hrm/hrm-server.pfx \
  -clcerts -nokeys |
  openssl x509 -outform DER |
  sha256sum
```

```bash
openssl pkcs12 \
  -in /opt/vnta/shared/certificates/gateway/gateway-client.pfx \
  -clcerts -nokeys |
  openssl x509 -outform DER |
  sha256sum
```

Xác nhận SAN của HRM certificate:

```bash
openssl pkcs12 \
  -in /opt/vnta/shared/certificates/hrm/hrm-server.pfx \
  -clcerts -nokeys |
  openssl x509 -noout -ext subjectAltName
```

Kết quả phải có:

```text
DNS:hrm-test.vnta.online, DNS:hrm-web
```

## 4. Tạo HMAC secret

Trên Ubuntu, tạo một secret mới:

```bash
openssl rand -hex 32
```

Sao chép kết quả vào password manager và dùng nó đúng một lần trong `.env.production` ở bước sau. `GATEWAY_HMAC_KEY_ID` là nhãn không phải secret; dùng `gateway-2026-01` cho môi trường test đầu tiên.

## 5. Tạo và điền `.env.production`

Từ thư mục gốc repository trên Windows, upload file mẫu:

```powershell
scp .\deploy\ubuntu\.env.production.example `
  vns@192.168.1.218:/tmp/.env.production
```

Trên Ubuntu, chuyển line ending Windows sang Linux, đặt file và giới hạn quyền:

```bash
sed -i 's/\r$//' /tmp/.env.production
mv /tmp/.env.production /opt/vnta/shared/env/.env.production
chmod 600 /opt/vnta/shared/env/.env.production
nano /opt/vnta/shared/env/.env.production
```

Điền hoặc thay các giá trị sau. Thay mọi text trong dấu `<...>` bằng giá trị thật; không giữ `CHANGE_ME`.

```dotenv
COMPOSE_PROJECT_NAME=vnta-hrm-2026
DEPLOY_ROOT=/opt/vnta
TZ=Asia/Ho_Chi_Minh

# Tag image sẽ được publish-ubuntu-release.ps1 cập nhật khi deploy.
HRM_HTTPS_PORT=8443
HRM_PUBLIC_ORIGIN=https://hrm-test.vnta.online

HRM_CERT_DIR=/opt/vnta/shared/certificates/hrm
GATEWAY_CERT_DIR=/opt/vnta/shared/certificates/gateway
HRM_TLS_CERT_PASSWORD='<password của hrm-server.pfx>'
GATEWAY_CLIENT_CERT_PASSWORD='<password của gateway-client.pfx>'

DATABASE_HOST=192.168.1.199
DATABASE_PORT=5432
DATABASE_NAME=<ten database>
DATABASE_USERNAME=<postgres user>
DATABASE_PASSWORD='<password PostgreSQL>'
DATABASE_TIMEZONE=Asia/Ho_Chi_Minh

HRM_DB_CONNECTION='Host=192.168.1.199;Port=5432;Database=<ten database>;Username=<postgres user>;Password=<password PostgreSQL>;Timezone=Asia/Ho_Chi_Minh'
ADMS_DB_CONNECTION='Host=192.168.1.199;Port=5432;Database=<ten database>;Username=<postgres user>;Password=<password PostgreSQL>;Timezone=Asia/Ho_Chi_Minh'

ADMS_CORE_API_ENABLED=true
ADMS_CORE_API_BASE_URL=https://hrm-web:8443

GATEWAY_CLIENT_CERT_SHA256_THUMBPRINT='<hash gateway ở bước 3>'
HRM_SERVER_CERT_SHA256_THUMBPRINT='<hash HRM ở bước 3>'
GATEWAY_HMAC_KEY_ID=gateway-2026-01
GATEWAY_HMAC_SECRET='<kết quả openssl rand ở bước 4>'
```

Hai connection string bắt buộc phải nằm trong dấu nháy đơn vì có dấu `;` và script deploy đọc file này bằng Bash. Password/HMAC có `#`, `;` hoặc khoảng trắng cũng phải được quote.

Lưu file trong `nano` bằng `Ctrl+O`, `Enter`, `Ctrl+X`.

### 5.1. Thay đổi password hoặc secret sau khi đã tạo file

Mở lại file bằng user `vns`:

```bash
nano /opt/vnta/shared/env/.env.production
```

Để sửa một dòng, nhấn `Ctrl+W`, gõ tên biến, ví dụ `DATABASE_PASSWORD=`, rồi nhấn `Enter`. Khi con trỏ đang ở dòng cần sửa, nhấn `Ctrl+K` để xóa cả dòng và gõ dòng mới.

Khi đổi password PostgreSQL, phải cập nhật cùng một giá trị ở cả ba dòng sau:

```dotenv
DATABASE_PASSWORD='...'
HRM_DB_CONNECTION='...Password=...;...'
ADMS_DB_CONNECTION='...Password=...;...'
```

Khi đổi password của PFX, export/upload lại PFX nếu password bên trong PFX cũng thay đổi, rồi cập nhật một trong hai biến tương ứng: `HRM_TLS_CERT_PASSWORD` hoặc `GATEWAY_CLIENT_CERT_PASSWORD`. Việc chỉ thay password không làm thay đổi thumbprint; chỉ lấy hash lại khi certificate bên trong PFX đã thay đổi.

Khi đổi `GATEWAY_HMAC_SECRET`, deploy đồng thời HRM và Gateway bằng mode `HrmAndGateway`; nếu chỉ deploy `HrmOnly`, Gateway đang chạy vẫn dùng secret cũ và các request gateway sang HRM sẽ bị từ chối.

Nếu cần xóa toàn bộ nội dung để thay lại file theo block mới, trong `nano`:

1. Nhấn `Ctrl+_`, gõ `1,1`, rồi nhấn `Enter` để về đầu file.
2. Nhấn `Ctrl+^` (thường là `Ctrl+Shift+6`) để bắt đầu chọn. Nếu terminal không nhận tổ hợp này, dùng `Alt+A`.
3. Nhấn `Alt+/` để tới cuối file.
4. Nhấn `Ctrl+K` để cắt/xóa toàn bộ phần đã chọn.
5. Dán nội dung mới, rồi nhấn `Ctrl+O`, `Enter`, `Ctrl+X` để lưu và thoát.

`Ctrl+U` sẽ dán lại phần vừa cắt nếu xóa nhầm.

## 6. Kiểm tra trước khi chạy release script

Không in secret ra terminal. Chỉ kiểm tra sự tồn tại file, quyền file và placeholder:

```bash
test -s /opt/vnta/shared/env/.env.production
test -s /opt/vnta/shared/certificates/hrm/hrm-server.pfx
test -s /opt/vnta/shared/certificates/gateway/gateway-client.pfx
stat -c '%A %U:%G %n' /opt/vnta/shared/env/.env.production

if grep -q 'CHANGE_ME' /opt/vnta/shared/env/.env.production; then
  echo 'STOP: .env.production vẫn còn CHANGE_ME'
else
  echo 'OK: không còn CHANGE_ME'
fi
```

Sau khi các bước trên hoàn tất, server đã sẵn sàng cho PowerShell chạy:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\deploy\ubuntu\scripts\publish-ubuntu-release.ps1 `
  -DeploymentMode HrmOnly `
  -ServerHost 192.168.1.218 `
  -SshUser vns `
  -SshPort 22
```

Lệnh này vẫn yêu cầu Ubuntu kết nối được PostgreSQL `192.168.1.199:5432`; Windows Firewall chỉ nên cho phép IP Ubuntu vào port này và không được public/NAT PostgreSQL ra Internet.
