# 13 - Kiểm tra phạm vi, commit và bàn giao

## Đầu vào

Dán Feature Refactor Manifest, final scorecard, kết quả build/test và danh sách file mong đợi. Chỉ dùng sau khi các bước implement/verification liên quan đã hoàn thành.

## Prompt

Hãy hoàn tất work item {{feature.display_name}} theo AGENTS.md. Trước khi commit:

1. Chạy git status --short --branch và git diff --check.
2. Nếu work item có source/config/test refactor, xác nhận nhánh hiện tại đúng là branch.name mới, khác branch.base, base commit đã được ghi nhận và Branch Gate đã pass. Nếu không, không commit refactor trên base/nhánh cũ; báo blocker chính xác.
3. So sánh diff với source map/kế hoạch: chỉ stage file thuộc work item hiện tại, giữ nguyên thay đổi người dùng hoặc thay đổi ngoài scope.
4. Xác nhận lệnh build/test bắt buộc theo manifest đã chạy thành công. Nếu required check chưa chạy, fail, hoặc test failure không được chứng minh là baseline ngoài scope, không commit và báo blocker chính xác.
5. Xác nhận route/payload/auth/schema/business rule/data ownership chỉ đổi khi manifest/Decision Gate cho phép; Compatibility Ledger, source comment/XML documentation của code đã đổi và documentation liên quan đã cập nhật nếu cần.
6. Với source/config thay đổi hợp lệ, tạo một commit độc lập với message ngắn, mô tả chính xác work item. Không dùng git add -A khi worktree có thay đổi ngoài scope; không push hoặc tạo PR.

Nếu work item chỉ là audit/read-only hoặc không có file thuộc scope, không tạo commit rỗng. Nếu documentation-only thay đổi, chạy kiểm tra tĩnh phù hợp như git diff --check và kiểm tra link/path, sau đó commit documentation scope theo AGENTS.md nếu đó là work item hoàn tất.

Bàn giao bằng:

- outcome ngắn gọn và file/feature đã thay đổi;
- use case/invariant/compatibility đã bảo toàn hoặc thay đổi được duyệt;
- lệnh build/test/check đã chạy cùng pass/fail/skip;
- baseline failure ngoài scope nếu có;
- commit hash và commit message, hoặc lý do cụ thể không commit;
- rủi ro/Decision Gate còn mở;
- xác nhận không push.
