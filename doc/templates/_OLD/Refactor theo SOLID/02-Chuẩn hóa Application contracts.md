# Prompt — Chuẩn hóa Application contracts

```text
Bạn là senior .NET application architect. Hãy refactor Application layer cho feature dưới đây theo Interface Segregation, Dependency Inversion và CQRS nhẹ. Đây là tác vụ IMPLEMENT.

## Đầu vào
- Feature group / name: `PhuCap` / `PhuCapTrachNhiemCapBac` — Cấp bậc phụ cấp trách nhiệm
- Application root: `src/Vnta.HRM2026/Vnta.Hrm.Application/PhuCap/PhuCapTrachNhiem`
- API contracts cần giữ tương thích: `IPayrollResponsibilityAllowanceGradeConfigurationReadService.GetGradeConfigAsync` và `IPayrollResponsibilityAllowanceGradeConfigurationWriteService.SaveGradeAsync`; giữ DTO/request/result và route `/api/payroll/responsibility-allowance/grade-config`.
- Phạm vi nghiệp vụ được phép thay đổi: Không đổi; server vẫn là nguồn chân lý cho validation mã/tên bậc, tiền chuẩn, thứ tự hiển thị, trạng thái sử dụng/ngừng dùng, optimistic concurrency, audit và JSON contract.

## Bắt buộc
1. Đọc AGENTS.md, kiểm tra git status và toàn bộ consumer trước khi sửa.
2. Đưa contracts vào `[Feature]/[Feature]/Contracts`, query models vào `Queries`, command request/result vào `Commands`, policy/calculator vào `Policies`, exception vào `Exceptions`.
3. Tách composite interface thành capability hẹp, ví dụ Read, Refresh/Recalculate, ManualAdjustment, Lock và Export nếu feature cần. Endpoint/provider chỉ phụ thuộc capability thực sự dùng.
4. Command chỉ nhận trường được phép thay đổi theo use case. Không để UI/API gửi derived value hoặc trường bị cấm chỉnh nếu server có thể tự tính.
5. Giữ JSON field name, endpoint behavior và compatibility. Chỉ giữ composite interface cũ nếu có consumer; đánh dấu Obsolete và nêu kế hoạch xóa.
6. Không đưa EF, HttpClient, database entity hay framework UI vào Application.
7. Cập nhật namespace, usages, DI registrations và test compile errors.

## Definition of Done
- Application contracts theo feature folder và không circular dependency.
- Mỗi consumer dùng narrow interface.
- Build project bị ảnh hưởng; chạy test feature liên quan.
- Báo cáo file cũ→mới, interfaces cũ/mới, compatibility giữ lại và test kết quả.
```
