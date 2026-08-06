# 12 - Kiểm định E2E SOLID và regression review

## Đầu vào

Dán Feature Refactor Manifest, tất cả báo cáo/source map/matrix đã cập nhật, git diff và kết quả build/test hiện tại.

## Prompt

Hãy kiểm định lại toàn feature {{feature.display_name}} sau refactor. Đọc AGENTS.md và git status --short --branch. Mặc định đây là review read-only; chỉ sửa P0/P1 structural defect có bằng chứng khi manifest cho phép và không cần quyết định semantic mới. Nếu thực hiện safe fix, Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout trước khi edit.

Rà theo đường đi thực tế UI -> provider -> transport -> endpoint -> Application -> Infrastructure -> persistence/audit/test. Với mỗi use case, xác nhận:

- UI không biết HTTP/EF/business rule; provider interface là capability hẹp; DI không còn depend concrete không cần thiết.
- Contract/request/DTO không lộ entity, actor/tenant, derived field hoặc over-posting path.
- Route/verb/auth/error contract/consumer đã khớp Compatibility Ledger; endpoint không chứa business/persistence logic.
- Policy/rule/period có canonical owner; UI metadata không drift; server final validation vẫn tồn tại.
- Query/export server-side scope/paging/allowlist; command writer/transaction/final-state validation/concurrency/lock/projection sync đúng Writer and Invariant Matrix.
- Audit action, policy, masking và mechanism matched actual tracked/raw/bulk mutation.
- Comment/XML documentation của các file đã đổi khớp behavior hiện tại, nêu rõ ý định/invariant ở boundary quan trọng, không còn summary chung chung, comment lặp syntax, comment-out code, TODO không có ngữ cảnh hoặc thông tin nhạy cảm.
- rg xác minh không còn consumer, route, DI registration, DTO/legacy wrapper chết hoặc bị bỏ sót. Không xóa legacy nếu consumer chưa được chứng minh là hết.
- Tests thật sự phủ P0/P1; verification output không bị hiểu sai bởi baseline failure ngoài scope.

Xuất final scorecard SRP/OCP/LSP/ISP/DIP và Finding Register P0-P3. Với mỗi finding, ghi evidence path:line, impact, safe fix hoặc Decision Gate cần thiết. Chỉ tuyên bố đạt chuẩn khi không còn P0/P1 không được chấp nhận và mọi compatibility/verification bắt buộc có evidence.

Nếu thực hiện safe fix, chạy lại lệnh verification ảnh hưởng và báo riêng file/lý do. Không commit/push ở bước review này trừ khi nó là work item độc lập đã hoàn tất và AGENTS.md yêu cầu commit.
