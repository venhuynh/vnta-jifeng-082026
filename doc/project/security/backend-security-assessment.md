# Đánh Giá Bảo Mật Backend HRM

## Mục tiêu

Xác định, ưu tiên và theo dõi rủi ro bảo mật trong backend HRM trước và trong các đợt refactor. Tài liệu này ghi phương pháp và trạng thái đánh giá; các phát hiện cụ thể thuộc [sổ phát hiện](./backend-security-findings.md).

## Phạm vi hiện tại

- Host và endpoint: `src/Vnta.HRM2026/Vnta.Hrm.Web/`.
- Use case, contract và policy: `src/Vnta.HRM2026/Vnta.Hrm.Application/`.
- Identity, EF Core, PostgreSQL, migration và integration: `src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/`.
- Dữ liệu HRM nhạy cảm: hồ sơ nhân sự, tài khoản/quyền, chấm công, lương và phụ cấp.
- Kết nối dùng chung PostgreSQL với attendance gateway và luồng sync ngoài ứng dụng.

Ngoài phạm vi trừ khi được mở rộng rõ ràng: kiểm thử xâm nhập hạ tầng mạng, cấu hình máy chủ production thực tế và mã nguồn của hệ thống bên thứ ba.

## Phương pháp đánh giá

1. Lập bản đồ tài sản, luồng dữ liệu và ranh giới tin cậy.
2. Rà soát xác thực, phân quyền, ownership theo tenant/nhân sự và workflow nhạy cảm.
3. Rà soát input validation, persistence, upload/integration, lỗi và log.
4. Rà soát secret/configuration, dependency, migration, CI/CD và vận hành.
5. Ghi phát hiện theo mức `Critical`, `High`, `Medium`, `Low` hoặc `Informational`; xác nhận lại sau khi sửa.

## Bằng chứng cần thu thập

- Vị trí source, route/endpoint, policy, contract và migration liên quan.
- Cấu hình đã được làm sạch thông tin nhạy cảm.
- Kết quả kiểm thử tái lập được, không chứa PII, token hay connection string.
- Ảnh hưởng tới tính bí mật, toàn vẹn, sẵn sàng và tuân thủ dữ liệu nhân sự.

## Kết quả rà soát tĩnh ngày 2026-07-17

Đã rà soát source hiện hành của `Vnta.HRM2026`; chưa thực hiện kiểm thử động, pentest hạ tầng hoặc kiểm tra cấu hình production. Các phát hiện và mức độ dưới đây là cơ sở để lập sprint refactor, không thay thế kiểm thử sau khi sửa.

| Khu vực | Kết quả | Tham chiếu |
| --- | --- | --- |
| Secret và bootstrap | Có credential fallback trong source và demo administrator được khởi tạo từ code | `SEC-001`, `SEC-002` |
| Gateway inbound | Ba endpoint nhận dữ liệu gateway không áp dụng xác thực hoặc chữ ký request | `SEC-003` |
| Realtime monitor | SignalR hub công khai, đọc snapshot và broadcast cho mọi client | `SEC-004` |
| Phân quyền API | Attendance và Payroll chỉ yêu cầu người dùng đã đăng nhập, chưa áp policy theo nghiệp vụ | `SEC-005` |
| Chống brute-force/DoS | Login không cộng dồn lỗi lockout; chưa thấy rate limiter tại host | `SEC-006` |
| Kiểm chứng | Chưa có project test độc lập cho security/integration | `SEC-007` |

Chi tiết bằng chứng đã được làm sạch nằm trong [sổ phát hiện](./backend-security-findings.md). Sprint xử lý đầu tiên là [`sprint-023-backend-security-refactor`](../../sprints/Security/sprint-023-backend-security-refactor/sprint-plan.md).
