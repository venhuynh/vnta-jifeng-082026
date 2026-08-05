# Runbook lỗi và khả năng phục hồi

## Mục đích

Hướng dẫn hỗ trợ và vận hành xử lý lỗi HTTP/circuit mà không cần lấy stack trace,
connection string hay dữ liệu nhạy cảm từ người dùng.

## Health checks

| Endpoint | Ý nghĩa | Kết quả mong đợi |
| --- | --- | --- |
| `/health/live` | Tiến trình web còn chạy | `200 { "status": "live" }` |
| `/health/ready` | Web kết nối được PostgreSQL | `200 { "status": "ready" }`; `503` khi DB/dependency không sẵn sàng |

Các endpoint dùng cho load balancer, monitoring và kiểm tra vận hành. Không dùng
endpoint readiness như một health indicator phía UI; UI phải hiển thị lỗi theo
outcome request thực tế.

## Trace ID

- Mọi response có header `X-Trace-Id`.
- API failure chuẩn trả `application/problem+json`, gồm `code` và `traceId`.
- Người dùng chỉ cần cung cấp mã hỗ trợ/trace ID và thời điểm xảy ra lỗi.
- Support tìm trong Serilog theo `TraceId` hoặc `CorrelationId`; không yêu cầu
  người dùng gửi ảnh màn hình chứa dữ liệu nhân sự/mật khẩu.

## Metrics và alert

Ứng dụng phát `System.Diagnostics.Metrics` meter `VNTA.HRM.Resilience` với các counter:

| Metric | Ý nghĩa | Điều kiện alert đề xuất |
| --- | --- | --- |
| `hrm.request.failures` | Request lỗi đã được exception handler phân loại, có tag `code`, `status_code` | Tăng đột biến 5xx/`dependency-unavailable` trong 5 phút |
| `hrm.login.unavailable` | Login không thể gọi Identity/dependency | Lớn hơn 0 liên tục trong 5 phút |
| `hrm.readiness.failures` | `/health/ready` không kết nối được PostgreSQL | Lớn hơn 0 trong 2 lần kiểm tra liên tiếp |

Meter không tự gửi alert. Nền tảng triển khai (OpenTelemetry collector, Application Insights,
Prometheus hoặc tương đương) phải thu meter này và tạo rule theo bảng trên; không nhúng webhook
hay secrets của môi trường vào source code.

## Phản ứng theo lỗi

| Outcome | Hành động vận hành |
| --- | --- |
| `dependency-unavailable` / `dependency-timeout` | Kiểm tra `/health/ready`, PostgreSQL, mạng và DNS; không yêu cầu người dùng submit liên tục. |
| `unexpected-error` | Tra trace ID trong Serilog, tạo incident nếu lặp lại. |
| `429` | Kiểm tra hành vi client/rate limit; người dùng thử lại sau. |
| Circuit reconnect failed/rejected | Người dùng tải lại trang; kiểm tra SignalR/proxy/websocket nếu xảy ra hàng loạt. |

## Retry và command an toàn

- Client chỉ retry truy vấn đọc/idempotent: timeout 8 giây mỗi lần, tối đa 2 retry có giãn
  cách ngắn. Sau đó UI hiển thị thông báo và cho người dùng tự chọn `Thử lại`.
- Không retry POST/PUT/PATCH/DELETE tự động. Nút command chỉ mở lại sau khi request kết thúc;
  người dùng quyết định gửi lại.
- Nếu nghiệp vụ bắt buộc retry command, endpoint phải nhận idempotency key và backend phải
  lưu/replay outcome theo key. Không dùng in-memory cache cho cam kết này trong môi trường
  nhiều instance.

## Kiểm tra trước rollout

1. PostgreSQL available: `/health/live` và `/health/ready` cùng trả 200.
2. PostgreSQL unavailable: `/health/live` trả 200, `/health/ready` trả 503 và login
   vẫn giữ form với message an toàn.
3. Chặn mạng/WebSocket tạm thời trên một tab: modal reconnect hiện, sau đó tab có
   thể reconnect hoặc cho phép tải lại.
4. Kiểm tra một API 401, 403, 409 và 503: HTTP client nhận đúng error kind/message;
   không hiển thị raw exception.
