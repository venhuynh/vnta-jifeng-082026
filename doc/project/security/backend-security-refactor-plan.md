# Kế Hoạch Refactor Bảo Mật Backend

## Nguyên tắc

- Ưu tiên xử lý rủi ro truy cập trái phép dữ liệu, chiếm quyền tài khoản, thực thi mã, lộ secret và vi phạm ranh giới dữ liệu.
- Server là nơi kết luận cuối cùng về phân quyền, validation, business rule và persistence.
- Không gộp thay đổi bảo mật lớn với refactor nghiệp vụ không liên quan nếu điều đó làm khó review hoặc rollback.
- Mỗi thay đổi phải có kiểm chứng tương ứng trong [backend-security-verification.md](./backend-security-verification.md).

## Lộ trình áp dụng cho HRM hiện tại

| Ưu tiên | Mục tiêu | Phát hiện | Đầu ra bắt buộc |
| --- | --- | --- | --- |
| P0 | Loại bỏ credential và bootstrap admin khỏi runtime/source | `SEC-001`, `SEC-002` | Credential cũ được xoay vòng; app fail-closed khi thiếu secret; không có demo admin/pre-filled password trong production path |
| P0 | Bảo vệ kênh gateway và monitor realtime | `SEC-003`, `SEC-004` | Contract xác thực gateway; hub có policy và payload tối thiểu; test request/client không có quyền |
| P1 | Áp quyền theo capability cho Attendance và Payroll | `SEC-005` | Ma trận role-capability-route; policy tại endpoint và kiểm tra ownership tại service |
| P1 | Giảm brute-force và flood | `SEC-006` | Lockout, rate limiter và log/audit không lộ dữ liệu nhạy cảm |
| P1 | Tạo safety net trong CI | `SEC-007` | Integration test, secret scan và dependency scan chạy trong pipeline |

## Thứ tự triển khai

1. **Dừng lộ credential trước khi merge code.** Chủ sở hữu hạ tầng phải xoay vòng credential database/AI nếu từng nằm trong source hoặc config đã track; việc này cần hoàn thành ngoài repository trước khi đánh dấu `SEC-001` hay `SEC-002` là mitigated.
2. **Chốt giao thức gateway.** Đội gateway và backend chọn mTLS hoặc HMAC có `timestamp`/`nonce`; không thay endpoint public bằng một API key hard-code khác.
3. **Siết realtime boundary.** Áp policy thiết bị vào hub, dùng group theo capability, loại bỏ raw payload không cần thiết và giới hạn kết nối/message.
4. **Tách policy API theo domain.** Không dùng `RequireAuthorization()` chung cho Payroll, Attendance hoặc lệnh thiết bị. Endpoint destructive phải có capability riêng; workflow self-service phải xác minh actor ownership trong Application/Infrastructure.
5. **Harden identity và host.** Đã bỏ `DemoData`, bật lockout/password policy/rate limiter; tiếp tục chốt request-size limit, cookie security và xử lý lỗi an toàn theo môi trường.
6. **Đóng bằng test.** Tạo integration tests cho `401`, `403`, role/capability, hub handshake, gateway signature/replay và app không khởi tạo user ở runtime.

## Phân công theo layer

- `Web`: middleware, endpoint boundary, authentication, authorization, HTTP hardening và error response.
- `Application`: policy abstraction, authorization theo nghiệp vụ, validation, transaction boundary và audit contract.
- `Infrastructure`: Identity store, EF Core query/configuration, migration, secret provider, integration client và logging sink.
- Database/operation: quyền DB tối thiểu, constraint, backup/restore, deployment configuration và giám sát.

## Theo dõi công việc

- Rủi ro kiến trúc dài hạn: thêm vào `doc/project/refactor-gap-register.md` với liên kết `SEC-###`.
- Công việc triển khai có phạm vi rõ: tạo sprint tại `doc/sprints/Security/sprint-###-slug/`.
- Chỉ đánh dấu `Verified` khi có kiểm chứng sau thay đổi, không chỉ dựa vào review mã nguồn.
- Kế hoạch thực thi chi tiết hiện tại: [`doc/sprints/Security/sprint-023-backend-security-refactor/`](../../sprints/Security/sprint-023-backend-security-refactor/sprint-plan.md).
