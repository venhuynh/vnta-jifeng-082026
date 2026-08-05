# Prompt — Khảo sát kiến trúc và SOLID

```text
Bạn là software architect .NET/Blazor. Hãy audit feature dưới đây từ UI đến Infrastructure và Tests. Đây là tác vụ PHÂN TÍCH; không sửa source code.

## Đầu vào
- Feature group: `KhauTru`
- Feature name: `KhauTruPhiCongDoan`
- UI root: `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/KhauTru/KhauTruPhiCongDoan`
- Route: `/payroll/union-fee-deductions`
- API root dự kiến: `/api/payroll/union-fee-deductions`
- Authorization policy: `InternalAccountPolicies.PayrollAdministration`
- Hành vi cần truy vết: chuẩn bị/tải kỳ lương, tìm kiếm-phân trang, tính lại, chỉnh số tiền thủ công, khóa/mở khóa từng dòng hoặc hàng loạt, xem bảng công tháng chỉ đọc và xuất Excel/PDF.

## Bắt buộc
1. Đọc toàn bộ AGENTS.md áp dụng; kiểm tra git status và không ảnh hưởng thay đổi sẵn có.
2. Dùng rg để lập dependency map đầy đủ: route → page/component → từng nút/event UI → provider → HTTP service → endpoint/method/URL → application contract → infrastructure/EF → test.
3. Liệt kê source thuộc feature, source dùng chung và consumer ngoài feature. Không suy đoán ownership.
4. Đánh giá SRP/OCP/LSP/ISP/DIP bằng bằng chứng file và dòng code.
5. Tìm các lỗi/rủi ro: khác biệt UI-backend, command quá rộng, policy nằm trong persistence, duplicate rule, race/concurrency, authorization/audit, migration/schema, performance, dead code/compatibility alias.
6. Phân loại P0/P1/P2 và đề xuất structure feature-first theo template 00. Không đề xuất thay đổi nghiệp vụ khi chưa có bằng chứng.

## Kết quả bắt buộc (tiếng Việt)
- Sơ đồ luồng và bảng từng nút UI đến backend.
- Bảng đánh giá SOLID, bằng chứng và tác động.
- Bảng file hiện tại → thư mục đích dự kiến.
- Backlog refactor theo thứ tự an toàn, dependency và rủi ro.
- Danh sách câu hỏi nghiệp vụ cần xác nhận.
```
