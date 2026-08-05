# Bảo mật backend HRM

Thư mục này là nguồn tài liệu sống cho việc đánh giá và refactor bảo mật backend của JIFENG HRM. Phạm vi bao gồm `Vnta.Hrm.Web`, `Vnta.Hrm.Application`, `Vnta.Hrm.Infrastructure`, PostgreSQL, ASP.NET Core Identity và các tích hợp backend liên quan.

## Tài liệu chính

- [Đánh giá bảo mật backend](./backend-security-assessment.md): phạm vi, phương pháp và trạng thái đánh giá.
- [Sổ phát hiện bảo mật](./backend-security-findings.md): phát hiện, mức độ rủi ro, bằng chứng đã được làm sạch và trạng thái xử lý.
- [Kế hoạch refactor bảo mật](./backend-security-refactor-plan.md): lộ trình xử lý theo lớp kiến trúc và mức ưu tiên.
- [Kiểm chứng bảo mật](./backend-security-verification.md): tiêu chí nghiệm thu sau refactor.
- [Contract gateway inbound](./gateway-inbound-contract.md): rollout mTLS, HMAC chống replay và xoay vòng key.
- [Ma trận route–capability](./backend-route-capability-matrix.md): policy, action và ranh giới dữ liệu tại API.
- [Runbook rate-limit và load test](./rate-limit-alert-and-load-test-runbook.md): alert vận hành và tiêu chí kiểm thử tải.
- [Runbook cấp tài khoản ban đầu](./initial-account-provisioning-runbook.md): cấp tài khoản có tách nhiệm vụ và audit, không bootstrap runtime.

## Quy tắc lưu trữ

- Không lưu secret, mật khẩu, token, connection string, khóa riêng, dữ liệu nhân sự thật hoặc ảnh chụp chứa các giá trị này.
- Bằng chứng phải dùng placeholder, mã định danh đã ẩn danh hoặc vị trí source tương đối.
- Mỗi phát hiện phải có chủ sở hữu, mức độ ưu tiên, trạng thái và liên kết đến commit/PR hoặc sprint xử lý.
- Đây là tài liệu cấp dự án; task triển khai cụ thể được lưu trong `doc/sprints/Security/`.
