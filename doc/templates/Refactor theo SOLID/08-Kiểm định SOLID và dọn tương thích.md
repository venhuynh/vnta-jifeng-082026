# Prompt — Kiểm định SOLID và dọn tương thích

```text
Bạn là principal engineer reviewer. Hãy kiểm định feature dưới đây sau refactor, sửa các vấn đề cấu trúc còn lại trong phạm vi an toàn và xác nhận Definition of Done. Đây là tác vụ IMPLEMENT + REVIEW.

## Đầu vào
- Feature group / name: `KhauTru` / `KhauTruPhiCongDoan` — Khấu trừ phí công đoàn
- Structure đích: Feature-first dưới `Components/KhauTru/KhauTruPhiCongDoan`, `Application/KhauTru/KhauTruPhiCongDoan`, `Infrastructure/KhauTru/KhauTruPhiCongDoan`, endpoint/API/provider tương ứng và test theo feature.
- Route/API cần giữ: `/payroll/union-fee-deductions` và `/api/payroll/union-fee-deductions/*`; authorization `InternalAccountPolicies.PayrollAdministration`.
- Breaking changes được phép: Không; giữ UX, JSON/schema, status code, lock/manual/concurrency/audit behavior.

## Bắt buộc
1. Đọc AGENTS.md, kiểm tra git diff/status và không đụng thay đổi ngoài feature.
2. Dùng rg xác minh source feature không còn rải rác; phân biệt hợp lý shared code với misplaced code.
3. Review SRP/OCP/LSP/ISP/DIP bằng code hiện tại, không chỉ dựa vào tên folder.
4. Kiểm tra mọi route/nút UI → provider → API → contract → implementation; DI không trỏ class cũ/dead registration.
5. Tìm duplicate code, obsolete alias, unused using/type/file. Chỉ xóa khi `rg` xác nhận không còn usage và việc xóa nằm trong scope.
6. Giữ public API/schema/UX; nếu legacy wrapper còn consumer, giữ nó với `[Obsolete]` và backlog xóa có điều kiện.
7. Chạy formatter/analyzer theo repo nếu có, build các project bị ảnh hưởng và test feature/full relevant suite.

## Báo cáo cuối cùng (tiếng Việt)
- Kết quả kiểm định SOLID theo từng nguyên tắc.
- Bảng đường dẫn canonical và compatibility còn lại.
- Những gì đã dọn, những gì cố ý giữ và lý do.
- Kết quả build/test chính xác.
- Backlog P0/P1/P2 còn lại, đặc biệt các quyết định nghiệp vụ không được tự ý đổi.
```
