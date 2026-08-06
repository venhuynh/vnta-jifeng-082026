# 07 - Refactor Infrastructure: CQRS thực dụng và persistence

## Đầu vào

Dán Feature Refactor Manifest, source map, Writer and Invariant Matrix, contract/policy đã chốt, và lát {{USE_CASE_SLICE}}. Nêu rõ schema/migration có được phép hay không.

## Prompt

Hãy refactor Infrastructure của {{feature.display_name}} cho lát {{USE_CASE_SLICE}}. Đọc AGENTS.md và git status --short --branch. Chỉ code khi Decision Gate tương ứng đã được duyệt. Trước source/config edit, Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout; nếu worktree bẩn hoặc tên nhánh đã tồn tại, không reset/stash/tái sử dụng mà báo blocker.

Tách theo responsibility thực:

- Read/query service tạo projection allowlist, AsNoTracking khi chỉ đọc, server-side filter/scope/paging/count, deterministic order và không trả entity tracked qua Application.
- Export dùng request/DTO/scope riêng, giới hạn volume phù hợp, allowlist cột và sanitize nội dung có thể thành formula khi định dạng đích cần điều đó.
- Command service sở hữu mutation của aggregate. Thực hiện final-state validation, đọc/claim record theo concurrency token, enforce tenant/ownership/lock server-side và trả result đã reload.
- Nếu command chạm nhiều record/bảng/projection trong cùng invariant, dùng transaction; failure validation/stale/lock phải không để partial write.
- Không để generic persistence hoặc consumer feature khác ghi derived field do feature này sở hữu. Đồng bộ projection trong transaction hoặc qua cơ chế đã được phê duyệt và test.
- Với tracked SaveChanges, kiểm tra audit interceptor/policy capture đúng entity/property. Với ExecuteUpdate/raw/bulk write, dùng audited mutation/operation audit tương đương, cùng transaction khi cần.
- Cấu hình entity/index/concurrency token/foreign key rõ. Không tự tạo migration nếu manifest cấm; nếu schema hiện không đủ để đảm bảo invariant, báo Decision Gate kèm migration plan.
- Đặt DI extension gần feature; composition root chỉ gọi extension, không biết implementation chi tiết.
- Với logic persistence không hiển nhiên, comment phải giải thích transaction boundary, write order/projection sync, concurrency/lock/tenant guard, audit mechanism hoặc lý do query/filter/index; không lặp lại từng câu EF/SQL.

Bổ sung integration test phù hợp cho success, validation không partial write, stale token, lock/authorization scope, projection sync và audit. Không giả lập transaction bằng test unit nếu feature thật sự dùng database semantics.

Kết thúc bằng file map query/command/persistence/DI, transaction/concurrency design đã thực thi, evidence audit và kết quả build/test. Commit theo AGENTS.md nếu đây là work item độc lập đã hoàn tất.
