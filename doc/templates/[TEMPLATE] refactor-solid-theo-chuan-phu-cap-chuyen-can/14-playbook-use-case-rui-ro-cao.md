# 14 - Playbook use case rủi ro cao

## Đầu vào

Dán Feature Refactor Manifest, source map, Writer and Invariant Matrix, contract/policy đã chốt, và chọn đúng một playbook dưới đây. Chỉ áp dụng use case feature đích thực sự có; action không tồn tại ghi N/A, không tự tạo để giống Phụ cấp chuyên cần.

## Prompt chung

Hãy triển khai hoặc kiểm định riêng use case {{USE_CASE_RUI_RO_CAO}} của {{feature.display_name}} trong phạm vi manifest. Đọc AGENTS.md và git status --short --branch. Trước code, xác nhận canonical writer, editable allowlist, transaction/lock/concurrency/audit và consumer compatibility trong Writer and Invariant Matrix. Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout; nếu không, dừng an toàn và báo blocker. Không thay đổi semantic, route, schema hoặc ownership chưa được phê duyệt.

Chọn một playbook phù hợp, rồi báo evidence theo bảng tương ứng.

### A. Điều chỉnh thủ công

- Chỉ cho phép field editable có trong command allowlist; derived/status/rule/amount do server tính không được nhận từ UI.
- Nếu nhiều field tạo một invariant, nhận chúng trong một command và validate final state. Không gọi tuần tự endpoint update từng field.
- Request có identifier + concurrency token; backend enforce tenant/ownership, lock detail/aggregate và reload result.
- Cập nhật projection liên quan trong một transaction; invalid/stale/locked không để partial write.
- Test success, invalid pair, stale token, locked, source-of-truth/projection, field-level audit/masking và UI feedback 409.

### B. Làm mới hoặc tính lại

- Xác định source data, input snapshot/config effective period và canonical writer trước khi overwrite kết quả.
- Enforce scope/authorization/lock/concurrency trên server; không để UI tính lại hay gửi derived result.
- Nếu refresh nhiều dòng, định nghĩa scope, batching/volume, cancellation/idempotency và audit operation; không làm partial state không thể xác định.
- Sau command, reload hoặc trả result canonical; test source missing, lock, concurrent refresh, audit và projection.

### C. Khóa hoặc mở khóa

- Định nghĩa chính xác selected rows, whole period/aggregate, null, empty và non-empty target semantics. Empty selection không được âm thầm thành all rows.
- Enforce lock ở mọi mutation liên quan, không chỉ endpoint lock. Lock/unlock phải có authorization, concurrency/version theo scope và audit.
- Xác định idempotency contract và response; bulk operation phải báo target succeeded/failed rõ ràng khi nghiệp vụ cần.
- Test selected/whole scope, stale target, already locked/unlocked, cross-tenant/scope và mutation bị chặn sau lock.

### D. Kỳ lương, filter và rule hiệu lực

- Tách toolbar/selected state khỏi applied query state. Action phải dùng context đã áp dụng, tránh export/mutation nhầm kỳ đang gõ dở.
- Period range, normalization/rejection, required fields và rule effective period có canonical policy server-side; UI chỉ hỗ trợ validation/presentation.
- Query và command dùng cùng semantics period. Test min/max, month/year invalid, boundary effective rule và pending filter state.

### E. Popup dữ liệu liên quan và export

- Popup chỉ xem phải là read-only, có server-side scope/authorization, allowlist DTO và không trở thành writer của nguồn dữ liệu.
- Export dùng server-side filter/scope, volume cap, deterministic output, column allowlist và xử lý formula injection khi cần.
- Test no mutation from related popup, unauthorized/out-of-scope read, export field/scope/format và audit operation.

Kết thúc bằng: playbook đã dùng, action-to-command map, invariant/test evidence, behavior/compatibility giữ lại và command build/test/commit theo AGENTS.md nếu work item độc lập hoàn tất.
