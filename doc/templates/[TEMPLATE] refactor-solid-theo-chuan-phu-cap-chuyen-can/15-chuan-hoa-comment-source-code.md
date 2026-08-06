# 15 - Chuẩn hóa comment source code và XML documentation

## Đầu vào

Dán Feature Refactor Manifest, source map, Writer and Invariant Matrix, danh sách use case đã refactor và git diff hiện tại. Áp dụng source_commenting trong manifest; nếu chưa được điền, dùng tiếng Việt có dấu theo convention repository và chỉ comment phần boundary/logic không hiển nhiên trong scope.

## Prompt

Hãy chuẩn hóa comment source code và XML documentation cho {{feature.display_name}} sau khi các boundary/use case đã ổn định. Đọc AGENTS.md và chạy git status --short --branch. Giữ nguyên thay đổi người dùng ngoài scope. Vì bước này sửa source, Branch Gate phải xác minh nhánh mới {{branch.name}} được tạo từ {{branch.base}} và đang được checkout trước khi edit; nếu không, dừng an toàn và báo blocker.

Đây là một quality pass. Chỉ sửa comment, XML documentation hoặc tài liệu kỹ thuật trực tiếp liên quan; không tự đổi behavior, public contract, route, JSON payload, authorization, schema, migration, DI hoặc format code chỉ để làm diff đẹp. Nếu comment phát hiện bug/semantic inconsistency, ghi Finding/Decision Gate với evidence thay vì sửa logic không được phép.

## Tiêu chuẩn comment bắt buộc

- Mỗi comment phải trả lời ít nhất một câu có giá trị: code này chịu trách nhiệm gì, vì sao cần tồn tại, invariant/precondition nào đang được bảo vệ, side effect/rủi ro nào phải lưu ý, hoặc consumer nào bị ràng buộc compatibility.
- Không lặp lại điều đã rõ từ tên type/method/biến hoặc syntax. Không tạo summary chung chung như “hỗ trợ xử lý dữ liệu”, “thành viên được sử dụng”, “gọi hàm để xử lý” nếu không chỉ ra use case và trách nhiệm thật.
- Comment phải đúng với source hiện tại. Xóa hoặc viết lại comment mâu thuẫn với canonical writer, authorization, rule, transaction, lock, concurrency, audit hoặc response behavior; comment sai trong các nội dung này là finding P1, không chỉ là style.
- Không có code bị comment-out, secret/token/PII, hướng dẫn bypass security, TODO/FIXME/HACK vô chủ hoặc ghi chú lịch sử mà Git đã lưu. Workaround hợp lệ phải nêu vấn đề, lý do, phạm vi và điều kiện xóa/ticket nếu có.
- Dùng một ngôn ngữ theo manifest/repository. Với repository này, comment mới mặc định là tiếng Việt có dấu; tên API/type/code giữ nguyên theo source.
- Chi tiết tỷ lệ với độ phức tạp: code đơn giản tự mô tả không cần comment; logic nghiệp vụ hoặc cross-layer không rõ phải được giải thích đủ để người mới hiểu mà không đọc toàn repository.

## XML documentation cho C#

- Bổ sung hoặc chỉnh /// &lt;summary&gt; cho public/cross-assembly/exposed contract có semantics không tự hiển nhiên trong scope: interface, class, record/DTO, enum, method, property hoặc event tạo boundary cho consumer. Các entry point internal nhưng đại diện HTTP/use case quan trọng cũng cần mô tả đủ rõ; không tạo XML docs boilerplate cho member tự mô tả.
- Dùng &lt;param&gt;, &lt;returns&gt;, &lt;exception&gt;, &lt;see cref&gt; và &lt;paramref&gt; khi chúng bổ sung semantics thật: nullability, đơn vị, timezone, paging/filter, cancellation, valid range, side effect, failure hoặc concurrency. Không thêm tag rỗng chỉ để đủ form.
- Contract command phải nêu editable allowlist, canonical owner/derived output và concurrency/lock behavior nếu không hiển nhiên. Query/export contract phải nêu scope, paging/filter, output allowlist hoặc giới hạn quan trọng.

## Comment theo layer

| Layer | Nội dung cần làm rõ khi áp dụng |
| --- | --- |
| Blazor/Razor | Page/state owner, lifecycle, loading/error/empty/selection state, EventCallback, cancellation, stale-response guard, double-submit/concurrency UX. Dùng @* *@ cho comment chỉ dành cho source; không render comment kỹ thuật ra DOM. |
| Client provider/HTTP | UI action/capability, route/verb/DTO mapping, cancellation/error propagation, compatibility wrapper và lý do client không tự tính business rule. |
| Web endpoint | Authorization boundary, actor/tenant/correlation lấy từ server, payload validation/allowlist, HTTP error map và legacy contract nếu còn consumer. |
| Application/policy | Use case, responsibility, canonical rule/source of truth, invariant, effective period/rounding/validation semantics. |
| Infrastructure/persistence | Transaction boundary, write order/projection sync, optimistic concurrency, lock/tenant checks, audit mechanism, query projection/filter/index/performance rationale nếu không hiển nhiên. |
| Test | Chỉ comment fixture/arrangement, data nghiệp vụ, external dependency hoặc regression rationale phức tạp. Tên test và assertion vẫn phải tự mô tả. |

## Quy trình

1. Dùng source map và git diff để tạo Comment Map: file:symbol, use case/layer, fact non-obvious cần giải thích, loại comment XML/C#/Razor, evidence và trạng thái Need/Existing/N/A.
2. Đọc source xung quanh trước khi viết. Không suy đoán behavior từ tên; nếu chưa xác minh, ghi UNKNOWN/Finding thay vì biến giả định thành comment.
3. Ưu tiên boundary và logic có rủi ro cao: ownership/projection, manual adjustment, transaction, lock, concurrency, audit, authorization, effective rule/period, error mapping, cancellation và compatibility.
4. Bổ sung hoặc sửa comment tối thiểu nhưng đầy đủ. Không dùng comment để che method/class quá lớn; báo refactor backlog có evidence nếu comment không thể làm logic đủ rõ.
5. Rà lại tất cả comment bị ảnh hưởng trong diff để bảo đảm không stale sau refactor. Kiểm tra Razor comment không vô tình đi vào DOM, XML docs không sai param/return/exception, source comment không có đường dẫn local hoặc số dòng biến động và diff không vô tình reformat/reorder code.
6. Chạy build/test phù hợp cho project source đã chạm theo manifest/AGENTS.md. Khi work item chỉ thay comment trong source, build vẫn cần chạy để kiểm chứng syntax/tooling; không báo pass nếu chưa chạy.

## Definition of Done và báo cáo

- Có Comment Map: File:symbol | use case/layer | fact non-obvious | loại comment | trạng thái Need/Existing/N/A | evidence path:line.
- Public/exposed boundary trong scope có XML documentation chính xác và hữu ích; logic non-obvious có comment về ý định/lý do, không phải bản dịch syntax.
- Đã kiểm tra không còn comment generic, stale, duplicate, comment-out code, TODO vô chủ hoặc dữ liệu nhạy cảm trong scope; phần intentionally N/A có lý do rõ.
- Không đổi behavior/contract/schema/route/DI chỉ để thêm comment; Finding/Decision Gate được nêu riêng nếu phát hiện inconsistency.
- Báo cáo file thay đổi, comment đã thêm/sửa/xóa và lý do, finding/backlog còn lại, lệnh build/test/check cùng kết quả thật.
- Nếu đây là work item độc lập và source đã đổi, stage đúng scope rồi commit theo AGENTS.md; không push.
