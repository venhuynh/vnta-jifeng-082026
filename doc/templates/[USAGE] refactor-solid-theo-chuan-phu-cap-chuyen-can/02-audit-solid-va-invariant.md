# 02 - Audit SOLID, invariant và rủi ro xuyên tầng

## Đầu vào

Dán Feature Refactor Manifest và source map của bước 01.

## Prompt

Hãy audit read-only feature {{feature.display_name}} theo source map. Đọc AGENTS.md, kiểm tra git status --short --branch, và không thay đổi source/config/migration/tài liệu/git history.

Đánh giá theo chuẩn Phụ cấp chuyên cần nhưng căn cứ trên source hiện có, không gán lỗi chỉ vì feature không có cùng folder hoặc cùng use case.

Lập SOLID scorecard với evidence path:line:

| Layer | SRP | OCP | LSP | ISP | DIP | Vấn đề | Mức độ | Cách xử lý ít rủi ro |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |

Phân tích bắt buộc:

- UI có chứa HTTP, EF/SQL, entity persistence, rule tính toán, mapping transport hoặc state không thuộc page owner không.
- Comment/XML documentation của source có giải thích đúng responsibility, canonical writer, invariant, authorization, transaction, lock, concurrency, audit và compatibility hay chỉ lặp lại syntax; comment sai hoặc lỗi thời có thể làm người sửa sau thay đổi behavior không.
- Interface/provider/service có capability quá rộng, phụ thuộc concrete implementation hoặc abstraction không có consumer thực tế không.
- Query, export và command có bị trộn; server có lọc, paging, ordering, allowlist output và giới hạn export không.
- Application có phụ thuộc UI/HTTP/EF không; policy/rule/period có bị lặp literal giữa UI/API/Infrastructure không.
- Endpoint có authorization ở đúng scope, lấy actor/tenant/correlation từ server, validate null/shape, map lỗi domain sang HTTP không.
- Mỗi command có writer/source-of-truth rõ, field derived/projection bị ghi bởi writer khác hay không.
- Có mutation nhiều bước hay nhiều endpoint tuần tự khiến partial write; có transaction, final-state validation, lock, optimistic concurrency và reload result không.
- Audit có thật sự capture entity/property/operation bị đổi, nhất là raw/bulk write và field nhạy cảm không.
- UI async có cancellation/dispose, stale response protection, disabled state phù hợp và error recovery không.
- Mỗi rủi ro có test hiện hữu/chưa có: policy, request, integration, endpoint authorization/contract, provider mapping và audit.

Phân loại finding:

- P0: có thể sai dữ liệu, bypass authorization/tenant, mất audit, partial write, dual writer hoặc data exposure.
- P1: có khả năng regression, sai contract, comment/documentation mô tả sai ownership/authorization/transaction/concurrency, lock không đáng tin, rule drift hoặc performance rõ rệt.
- P2: vi phạm boundary/SOLID hoặc source documentation thiếu ở public/cross-layer boundary có tác động đáng kể đến bảo trì, review hoặc testability.
- P3: naming, duplication nhỏ, comment/style không làm sai cách hiểu behavior.

Kết thúc bằng audit register theo ưu tiên, nêu rõ finding nào chỉ có thể sửa khi manifest phê duyệt route, schema, business rule hoặc ownership. Không đề xuất một "service chung" hoặc interface chỉ để tăng số abstraction.
