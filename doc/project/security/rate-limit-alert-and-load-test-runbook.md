# Runbook alert rate limit và kiểm thử tải

`Vnta.Hrm.Web` ghi event từ chối rate limit với category `Security.RateLimiting`, các trường `Path`, `Method` và `RetryAfterSeconds`. Event không ghi username, password, token hay IP để tránh đưa dữ liệu nhạy cảm vào log alert.

## Cấu hình vận hành bắt buộc

1. Thu log JSON của HRM vào hệ thống tập trung; không alert trực tiếp từ file container.
2. Tạo alert warning khi có từ 20 event `Security.RateLimiting` trong 5 phút cho `/Account/Login`.
3. Tạo alert warning khi có từ 60 event trong 5 phút cho `/api/integration/`; đồng thời kiểm tra health của attendance gateway.
4. Tạo alert critical khi alert trên kéo dài 15 phút hoặc tỷ lệ `5xx` vượt 2% trong cùng cửa sổ.
5. Alert chỉ gửi route, count, thời gian và environment; không đưa request body/header/secret vào nội dung thông báo.

## Kiểm thử tải trước production

- Chạy ở staging, bằng credential/certificate/key thử nghiệm độc lập.
- Xác nhận login bị `429` sau 10 request/phút theo cùng IP, có `Retry-After`, và login hợp lệ hoạt động lại sau cửa sổ limit.
- Xác nhận gateway vượt 120 request/phút nhận `429`; request HMAC/mTLS hợp lệ trong ngưỡng vẫn thành công.
- Theo dõi latency p95, tỷ lệ `5xx`, CPU, memory, connection pool và log rate-limit trong tối thiểu 15 phút.
- Không gửi PII, raw biometric payload hay credential production vào công cụ load test.

Kết quả thực thi (thời gian, môi trường, công cụ, ngưỡng, người xác nhận) phải được lưu ở implementation log của nhánh theo format `yyyyMMdd-<branch>.md`.
