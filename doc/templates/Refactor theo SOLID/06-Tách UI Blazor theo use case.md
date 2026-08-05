# Prompt — Tách UI Blazor theo use case

```text
Bạn là senior Blazor UI architect. Hãy refactor UI feature dưới đây thành các component/coordinator nhỏ theo SOLID, giữ nguyên UX, route và behavior. Đây là tác vụ IMPLEMENT.

## Đầu vào
- Feature group / name: `KhauTru` / `KhauTruPhiCongDoan` — Khấu trừ phí công đoàn
- UI root: `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/KhauTru/KhauTruPhiCongDoan`
- Route cần giữ: `/payroll/union-fee-deductions`
- Authorization cần giữ: `[Authorize(Policy = InternalAccountPolicies.PayrollAdministration)]`.
- Use case/UI flow: chọn tháng/năm và tải dữ liệu; tìm kiếm/phân trang/chọn dòng; chỉnh phí; khóa/mở khóa dòng hoặc hàng loạt theo dòng đã chọn/toàn kỳ; tính lại có xác nhận; xem quy tắc; đối chiếu bảng công tháng chỉ đọc; xuất Excel/PDF.
- UI/UX được phép thay đổi: Không; giữ loading/cancellation, disabled state, validation tiền, toast/error, selection và cảnh báo concurrency/locked row.

## Bắt buộc
1. Khảo sát từng control/nút/event và map tới provider/backend trước khi tách.
2. Giữ page host mỏng: route, authorization, compose sections và coordinator cấp cao.
3. Tách source theo trách nhiệm vào `Sections`, `Dialogs`, `State`, `Models`, `Export`.
4. Child component nhận parameter/event callback hoặc state contract rõ ràng; không trực tiếp truy cập DbContext/endpoint/concrete infrastructure.
5. Data provider nằm `Services/DataProviders/[Feature group]/[Feature name]`; HTTP client nằm `Services/Api/[Feature group]/[Feature name]`.
6. Giữ loading, cancellation, selection, validation, error/toast, optimistic concurrency và disabled state. Không tạo race do concurrent render/event.
7. Không tách chỉ vì giảm số dòng: mỗi component phải có use case/ownership rõ ràng. Nếu partial class an toàn hơn, tách partial theo use case và ghi backlog.
8. Tìm và xử lý tất cả usages trước khi di chuyển UI model. Shared UI model không được ép vào feature folder nếu feature khác dùng.

## Definition of Done
- Component/page không còn god-class; mỗi section/dialog có trách nhiệm rõ.
- Không thay route, authorization hoặc flow nút đã có.
- Build client/web; chạy UI/provider tests phù hợp.
- Báo cáo component/file cũ→mới và map từng nút UI → provider/API sau refactor.
```
