# Contract Gateway Inbound: mTLS và HMAC

Áp dụng cho các endpoint dưới `/api/integration/`. HRM mặc định fail-closed: request không có client certificate và HMAC hợp lệ bị từ chối.

## Mô hình bảo vệ

1. **mTLS** xác thực attendance gateway tại kết nối TLS.
2. **HMAC-SHA-256** xác thực từng request và bảo vệ chống replay.
3. **Rate/body limit** giảm flood và payload quá lớn.

Không dùng API key tĩnh không ký request như cơ chế thay thế.

## Cấu hình secret runtime

Không ghi key vào `appsettings.json`. Cấu hình qua secret store hoặc environment:

```text
IntegrationSecurity__GatewayInbound__RequireMutualTls=true
IntegrationSecurity__GatewayInbound__Keys__gateway-2026-01=<secret>
IntegrationSecurity__GatewayInbound__TrustedClientCertificateSha256Thumbprints__0=<gateway-client-certificate-sha256>
```

Với file local đã ignore, xem `src/Vnta.HRM2026/Vnta.Hrm.Web/appsettings.Local.example.json`. Có thể giữ nhiều key trong `Keys` trong giai đoạn xoay vòng, nhưng gateway chỉ gửi một `key-id` đang hoạt động.

## Header bắt buộc

| Header | Giá trị |
| --- | --- |
| `X-VNTA-Key-Id` | ID key đang hoạt động, ví dụ `gateway-2026-01` |
| `X-VNTA-Timestamp` | Unix timestamp UTC tính bằng giây |
| `X-VNTA-Nonce` | Giá trị ngẫu nhiên, duy nhất cho mỗi request |
| `X-VNTA-Signature` | HMAC-SHA-256 dạng hexadecimal viết hoa hoặc thường |

## Canonical request

Gateway tạo chuỗi UTF-8, mỗi thành phần cách nhau một ký tự newline (`\n`):

```text
<HTTP_METHOD_UPPERCASE>
<PATH_BASE + PATH>
<X-VNTA-Timestamp>
<X-VNTA-Nonce>
<SHA-256 raw request body dạng hexadecimal>
```

Ví dụ path: `/api/integration/adms/realtime/events`. `X-VNTA-Signature` là HMAC-SHA-256 của canonical request, dùng key tương ứng `X-VNTA-Key-Id`.

### Bảo toàn raw body tại HRM

HRM phải bật request buffering cho `/api/integration` trước endpoint binding, sau đó filter HMAC rewind stream về đầu trước khi tính hash. Nhờ vậy SHA-256 phía server luôn dùng đúng bytes mà gateway đã ký; không được hash JSON đã deserialize, body rỗng sau binding, hoặc payload đã được format lại.

## Rule server

- Clock skew tối đa: 300 giây.
- Nonce được giữ tối thiểu 600 giây; nonce trùng bị từ chối.
- Raw body tối đa: 1 MiB.
- Signature được so sánh constant-time.
- Gateway security chưa được cấu hình key nhận `503`; client không có certificate, key không tồn tại, signature sai hoặc request replay nhận `401`; payload quá lớn nhận `413`.
- Regression test phải bao phủ tối thiểu request thiếu credential, request HMAC hợp lệ đầu tiên được chấp nhận, và request dùng lại cùng nonce bị từ chối.

## mTLS deployment

- Kestrel được cấu hình `AllowCertificate`; filter inbound bắt buộc certificate khi `RequireMutualTls=true` và chỉ chấp nhận SHA-256 của raw certificate đã khai báo trong `TrustedClientCertificateSha256Thumbprints`. Không cấu hình hash hợp lệ nghĩa là fail-closed.
- Nếu TLS kết thúc ở reverse proxy, Kestrel không quan sát được client certificate. Ưu tiên TLS pass-through cho route gateway; chỉ dùng certificate forwarding khi proxy network và forwarded certificate validation đã được review riêng. Không tin header certificate do client tự gửi.
- CA tin cậy, certificate gateway và private key phải nằm trong secret store của host/gateway; certificate có hạn ngắn và có lịch xoay vòng.

## Docker production

Mẫu `deploy/ubuntu/docker-compose.production.yml` chạy HRM bằng HTTPS trên `8443`, không còn hỗ trợ luồng HTTP cho gateway. Chủ vận hành phải đặt các file sau ngoài repository, mount read-only qua các thư mục đã khai báo trong `.env.production`:

- `${HRM_CERT_DIR}/hrm-server.pfx`: certificate server; SAN phải chứa `hrm-web` để gateway trong Docker xác minh hostname, đồng thời chứa DNS/IP public nếu người dùng truy cập trực tiếp.
- `${GATEWAY_CERT_DIR}/gateway-client.pfx`: certificate client gửi từ attendance gateway.

Các biến `GATEWAY_CLIENT_CERT_SHA256_THUMBPRINT` và `HRM_SERVER_CERT_SHA256_THUMBPRINT` lần lượt là SHA-256 của raw certificate client/server. Gateway chỉ cho phép bỏ qua lỗi trust-chain khi certificate server đúng SHA-256 đã pin; lỗi hostname luôn bị từ chối. Không dùng callback chấp nhận mọi certificate.

Gateway tự tạo `X-VNTA-*` cho mọi lệnh gọi Core API. `GATEWAY_HMAC_SECRET`, mật khẩu PFX và các thumbprint chỉ được cấp ở runtime; giới hạn quyền đọc file certificate và `.env.production` cho tài khoản deploy.

## Rollout và xoay vòng

1. Chủ vận hành tạo DB credential mới, certificate gateway và HMAC key mới; lưu ngoài source control.
2. Đưa key mới vào HRM dưới key-id mới, vẫn giữ key cũ trong thời gian chuyển đổi ngắn.
3. Cấu hình gateway dùng certificate và key-id mới; gửi thử request hợp lệ qua HTTPS.
4. Kiểm tra request thiếu certificate, signature sai và nonce replay đều bị từ chối.
5. Xóa key/certificate/credential cũ, cập nhật audit record và xác nhận không còn kết nối dùng credential cũ.

## CI package source

GitHub Environment `ci-security` cần ba secret đọc package: `DEVEXPRESS_NUGET_SOURCE`, `DEVEXPRESS_NUGET_USERNAME`, `DEVEXPRESS_NUGET_PASSWORD`. Không cấp DB credential, HMAC key hoặc private certificate cho workflow này.
