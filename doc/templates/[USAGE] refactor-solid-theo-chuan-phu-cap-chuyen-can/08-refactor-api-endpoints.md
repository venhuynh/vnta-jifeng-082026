# 08 - Refactor Web API endpoints, security và audit boundary

## Đầu vào

Dán Feature Refactor Manifest, source map, Compatibility Ledger, Writer and Invariant Matrix, contract Application và lát {{USE_CASE_SLICE}}.

## Prompt

Hãy chuẩn hóa HTTP boundary cho {{USE_CASE_SLICE}} của {{feature.display_name}}. Đọc AGENTS.md, chạy git status --short --branch và chỉ sửa code trong scope đã được phê duyệt. Trước source/config edit, Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout; nếu không, dừng an toàn và báo blocker.

Áp dụng các yêu cầu sau:

- Có endpoint mapping feature-local. Route group áp authorization policy đúng scope; không dựa vào việc UI ẩn nút để bảo vệ thao tác.
- Tách query/read/export endpoint khỏi command endpoint khi responsibility/error semantics khác nhau. Không tạo endpoint mới chỉ để giống mẫu nếu use case không tồn tại.
- Endpoint phụ thuộc Application contract, không inject concrete Infrastructure/DbContext. Endpoint không chứa formula, EF query hoặc mutation business.
- Bind request nullable, kiểm tra null/shape cơ bản, gọi validator/Application, truyền CancellationToken và trả response DTO. Không bind entity persistence hoặc actor/tenant/sensitive derived field từ body.
- Actor, tenant, organization, correlation/audit context lấy từ authenticated server context. Không tin ID/role/scope do client gửi.
- Dùng error mapper nhất quán: validation bad request, not found, locked/concurrency conflict, forbidden/unauthorized và unexpected failure phải đúng HTTP contract hiện có. Không nuốt exception rồi trả success rỗng.
- Ghi audit action/correlation ở boundary phù hợp. Xác nhận Infrastructure có cơ chế capture mutation; audit scope đơn thuần không thay thế audit evidence.
- Giữ route, verb và JSON contract hiện có khi Compatibility Ledger ghi PRESERVE. Route legacy chỉ giữ khi có consumer/contract test; nếu thay public contract đã được duyệt, cập nhật consumer và test cùng lát.
- Comment endpoint/transport boundary chỉ khi cần làm rõ authorization, actor/tenant/correlation server-side, payload allowlist, status/error mapping hoặc compatibility. Không copy route/verb hiển nhiên thành prose dư thừa.

Bổ sung endpoint tests cho authorization, missing/invalid payload, request forwarding/allowlist, failure mapping, cancellation khi test harness hỗ trợ và compatibility contract. Dùng integration test khi authorization/audit policy không thể chứng minh bằng unit test.

Kết thúc bằng bảng route/verb/auth/contract trước-sau, map lỗi, audit evidence, consumer cập nhật và kết quả verification. Nếu work item độc lập đã hoàn tất, build/test/commit theo AGENTS.md; không push.
