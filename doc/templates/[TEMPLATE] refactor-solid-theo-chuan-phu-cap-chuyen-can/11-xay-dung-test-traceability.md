# 11 - Xây dựng test traceability và kiểm chứng

## Đầu vào

Dán Feature Refactor Manifest, source map cập nhật, Writer and Invariant Matrix, Compatibility Ledger và danh sách file/code đã đổi.

## Prompt

Hãy xây traceability matrix và bổ sung/chạy test cho {{feature.display_name}}. Đọc AGENTS.md và git status --short --branch. Chỉ sửa test/source cần thiết trong scope; không xóa test failing hay nới lỏng assertion để tạo pass giả. Nếu bước này thêm/sửa test hoặc source, Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout trước khi edit.

Tạo bảng tối thiểu:

| Use case | Invariant/rủi ro | Layer chứng minh | Test hiện có | Test cần thêm | Lệnh chạy | Kết quả |
| --- | --- | --- | --- | --- | --- | --- |

Tên test và assertion phải tự mô tả behavior. Chỉ comment fixture/arrangement phức tạp, dữ liệu đại diện nghiệp vụ, external dependency hoặc lý do regression; comment không thay thế assertion rõ nghĩa.

Phủ theo applicability, ghi N/A có lý do/evidence nếu feature không có action đó:

- Pure policy/validator: boundary, effective period, threshold, rounding, combination invalid, rule metadata.
- Query/read/export: tenant/scope, filter, paging/max size, deterministic sort, field allowlist, export formula sanitation/volume limit.
- Command/integration: success, invalid final state không partial write, canonical writer/projection sync, stale concurrency token, lock, selected-vs-whole scope, idempotency nếu có, audit evidence.
- Endpoint: authorization, missing/invalid payload, actor/scope không tin client, request forwarding, 400/404/409/error mapping, legacy contract còn consumer.
- Client/provider/UI: mapping, exact command payload, state transition loading/error/retry, concurrency/lock feedback, cancellation/stale response khi khả dụng.
- Regression: consumer/DI/route registrations, public compatibility và baseline test failure ngoài scope.

Chạy các lệnh test/build trong manifest và những test feature-specific phát hiện được. Nếu integration test cần database/container/secret không có, ghi SKIPPED kèm prerequisite chính xác, không báo pass. Nếu test project fail trước khi chạy tests do lỗi baseline ngoài scope, nêu file/error, tách nó khỏi kết quả test feature và không tự sửa nếu không thuộc scope.

Kết thúc bằng matrix có kết quả thật, lệnh đã chạy, coverage/rủi ro còn lại, và danh sách thay đổi test. Nếu work item độc lập đã hoàn tất với source/config thay đổi, commit theo AGENTS.md sau khi toàn bộ verification bắt buộc pass.
