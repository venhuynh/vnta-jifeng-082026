# 05 - Refactor Application contracts theo capability

## Đầu vào

Dán Feature Refactor Manifest, source map, audit register, kế hoạch đã được duyệt và Writer and Invariant Matrix. Xác định lát use case đang triển khai: {{USE_CASE_SLICE}}.

## Prompt

Hãy refactor Application boundary cho lát {{USE_CASE_SLICE}} của {{feature.display_name}} trong phạm vi manifest. Đọc AGENTS.md, kiểm tra git status --short --branch và bảo toàn thay đổi ngoài scope. Chỉ bắt đầu code nếu mode là STRUCTURAL_REFACTOR hoặc APPROVED_BEHAVIOR_FIX và mọi Decision Gate liên quan đã có trạng thái GO. Không bắt đầu source/config edit nếu Branch Gate chưa xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang là nhánh hiện tại; worktree bẩn hoặc nhánh đã tồn tại phải được báo blocker, không reset/stash/tái sử dụng.

Thiết kế hoặc chỉnh Application theo các quy tắc:

- Tạo interface hẹp theo capability/use case thực: ví dụ Read, Export, Refresh, ManualAdjustment, Lock hoặc tên tương đương. Không tạo generic repository/service framework chỉ để có abstraction.
- Command request chỉ chứa identifier, editable allowlist, scope cần thiết và concurrency token đã chốt. Query/filter/export request có model riêng. Không dùng EF entity, HttpContext, component model hoặc actor do client gửi.
- DTO/read model phản ánh output allowlist của use case, không biến persistence entity thành public contract. Command trả result/read DTO đã xác định, không trả tracked entity.
- Tách policy/calculator/validator thuần khi logic là business rule có thể test độc lập. Application không tham chiếu EF, HTTP, Blazor, Infrastructure implementation hoặc UI project.
- Chuẩn hóa domain/application failure cho validation, not found, locked, concurrency, forbidden/scope nếu cần; giữ HTTP mapping ở Web.
- Đọc consumer trước khi đổi signature. Chỉ giữ wrapper compatibility khi Compatibility Ledger yêu cầu, ghi rõ obsolete/exit plan và test contract.
- Cập nhật DI consumer map hoặc compile consumer cần thiết, nhưng không đưa EF/transport vào Application để né lỗi compile.
- Khi source thuộc scope thay đổi, XML documentation/comment phải làm rõ semantics contract không tự hiển nhiên: editable allowlist, canonical owner/derived output, valid scope, cancellation, failure, lock/concurrency hoặc compatibility. Áp dụng checklist chi tiết ở bước 15, không thêm boilerplate cho member tự mô tả.

Trước khi hoàn tất, rà bằng rg các implementation, endpoint/client consumer và DI registration của mọi contract đổi tên. Bổ sung unit test request validation/policy cho invariant thuộc layer này.

Nếu prompt này là một work item độc lập và đã đổi source/config, chạy build/test trong manifest phù hợp trước khi kết thúc và tạo commit chỉ chứa scope hoàn tất theo AGENTS.md. Nếu nó là một lát của cùng work item đa giai đoạn, không stage thay đổi ngoài lát và để bước 13 thực hiện commit cuối sau khi toàn bộ kiểm tra pass.

Báo cáo: contract trước/sau, field allowlist, consumer/compatibility impact, policy/validation ownership, test đã chạy và mọi gate còn lại.
