# 09 - Refactor client provider và HTTP transport

## Đầu vào

Dán Feature Refactor Manifest, source map, Application/Web contract đã chốt, Compatibility Ledger và lát {{USE_CASE_SLICE}}.

## Prompt

Hãy chuẩn hóa client boundary cho {{USE_CASE_SLICE}} của {{feature.display_name}}. Đọc AGENTS.md và git status --short --branch; chỉ sửa trong scope được phê duyệt. Trước source/config edit, Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout; nếu không, dừng an toàn và báo blocker.

Mục tiêu: component Blazor chỉ thấy capability phục vụ UI, transport chỉ thấy HTTP contract, và không có rule/persistence bị kéo lên client.

- Tạo hoặc thu gọn capability DataProvider theo use case thật. Mỗi component/page inject interface nhỏ nhất cần dùng; composite interface chỉ giữ khi Compatibility Ledger chứng minh consumer cũ cần nó.
- Typed HTTP adapter implements Application/client contracts, map route/verb/request/response chính xác, dùng cancellation token và helper error handling hiện hành. Không để component tự ghép URL, HttpClient request hay đọc JSON.
- Provider map transport DTO sang view record/presentation model. Mapping phải explicit cho field nhạy cảm/derived/concurrency token; không tự tính lại amount/rule/status do server sở hữu.
- Provider có thể mở audit command scope theo runtime kiến trúc Interactive Server nếu repository yêu cầu, nhưng server vẫn là authority cho actor/audit persistence. Không duplicate mutation để tạo audit ở hai nơi.
- Preserve selected/applied filter context và forwarding cancellation. Không cache hoặc retry mutation tự động nếu có thể lặp write; khi gặp 409/lock, để UI xử lý reload/feedback theo contract.
- Đổi interface/route phải tìm tất cả client/test consumer bằng rg, cập nhật DI feature-local và kiểm tra registration của typed adapter/capability.
- Không gửi field derived/read-only, entity ID ngoài scope, actor/tenant hoặc unapproved bulk target. Manual adjustment phải gọi một aggregate command theo Writer and Invariant Matrix.
- Comment provider/transport khi mapping payload, field omission, cancellation/error propagation, compatibility wrapper hoặc lý do không tính lại business value ở client không hiển nhiên; không mô tả lại lời gọi HTTP đơn giản.

Thêm test provider cho request forwarding, DTO-to-view mapping, atomic command payload, error/concurrency propagation và DI/contract consumer cần thiết. Test transport/http phù hợp nếu fake provider không thể phát hiện sai route/verb.

Kết thúc bằng map UI capability -> provider -> HTTP contract, compatibility note, DI evidence, test/build result và commit theo AGENTS.md nếu đây là work item độc lập đã hoàn tất.
