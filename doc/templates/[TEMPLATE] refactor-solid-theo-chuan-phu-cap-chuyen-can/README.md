# Bộ prompt refactor SOLID theo chuẩn Phụ cấp chuyên cần

## Mục đích

Bộ prompt này biến kiến trúc của màn hình Phụ cấp chuyên cần thành chuẩn tái sử dụng cho một feature khác, từ Blazor UI đến Infrastructure. Chuẩn ở đây là boundary, ownership dữ liệu, khả năng kiểm chứng và cách chia use case; không phải sao chép tên nghiệp vụ, route, field, công thức hoặc giao diện của Phụ cấp chuyên cần.

Một lần refactor bắt đầu bằng việc điền duy nhất manifest ở file 00. Các prompt sau dùng lại manifest đã điền và các báo cáo của bước trước. Không điền thông tin chưa biết bằng suy đoán: bước khảo sát sẽ tìm và đóng băng phần đó.

## Trình tự sử dụng

1. 00-feature-refactor-manifest.md: điền đầu vào, phạm vi và các giới hạn thay đổi.
2. 01-khao-sat-source-map.md: lập bản đồ source UI đến dữ liệu, không sửa mã.
3. 02-audit-solid-va-invariant.md: audit SOLID, bảo mật và invariant, không sửa mã.
4. 03-ke-hoach-refactor-va-decision-gate.md: chốt lát thay đổi và các quyết định cần phê duyệt, không sửa mã.
5. 04-data-ownership-atomicity-va-compatibility.md: chốt writer, projection, transaction, lock, concurrency, audit và compatibility trước khi code.
6. 05-refactor-application-contracts.md đến 10-refactor-ui-blazor.md: triển khai theo lát use case nhỏ; có thể lặp lại chuỗi này cho từng use case lớn.
7. 14-playbook-use-case-rui-ro-cao.md: prompt bổ sung tùy chọn cho điều chỉnh thủ công, tính lại, khóa, period/rule, popup read-only hoặc export.
8. 15-chuan-hoa-comment-source-code.md: chuẩn hóa comment/XML documentation sau khi boundary và use case đã ổn định, trước kiểm chứng cuối.
9. 11-xay-dung-test-traceability.md: bổ sung và chạy test theo traceability matrix.
10. 12-kiem-dinh-e2e-solid.md: rà lại toàn feature và các consumer.
11. 13-commit-va-ban-giao.md: kiểm tra phạm vi, commit hợp lệ và bàn giao.

Không bỏ qua bước 04 cho feature có mutation. Với feature chỉ đọc, ghi rõ N/A cho các cột mutation thay vì giả định chúng không tồn tại.

Đọc reference-phu-cap-chuyen-can.md khi cần đối chiếu pattern thực tế. Tài liệu này không phải một prompt triển khai và không thay thế source map của feature đích.

## Chuẩn Phụ cấp chuyên cần được kế thừa

- UI là lớp điều phối: page host, Sections, Dialogs, State, Models, Commands và Presentation có trách nhiệm rõ; component không truy cập EF/SQL hay tự tính business rule.
- UI phụ thuộc capability hẹp, ví dụ Read, Export, Refresh, ManualAdjustment, Lock; không inject concrete implementation hoặc một interface khổng lồ nếu chỉ cần một khả năng.
- Client provider/HTTP adapter chỉ map transport và view model; Application sở hữu contract/use case và policy thuần; Infrastructure sở hữu EF, query projection và persistence.
- Query/read model, export và command là các luồng tách biệt. Server chịu trách nhiệm filter, paging, authorization và dữ liệu trả về.
- Mỗi giá trị có một canonical writer. Giá trị chiếu sang aggregate/màn hình khác là projection hoặc read-only, không tạo dual writer.
- Một mutation thay đổi các field phụ thuộc nhau phải là một command atomic, kiểm tra invariant theo final state, có transaction nếu chạm nhiều record/bảng, lock và optimistic concurrency nếu nghiệp vụ cần.
- Rule, period và metadata hiển thị có source of truth tập trung; UI chỉ dùng metadata/validation UX, backend vẫn xác nhận cuối cùng.
- Endpoint mỏng, có authorization, audit context và error mapping; actor/tenant/ownership không tin từ request body.
- Audit phải capture đúng entity/property hoặc operation thực tế; raw/bulk write không được kỳ vọng interceptor tracked tự ghi audit.
- Test phải chứng minh policy, contract, source-of-truth, atomicity, lock, concurrency, audit, authorization và mapping quan trọng.

Các legacy compatibility wrapper trong Phụ cấp chuyên cần, như composite provider hoặc endpoint cập nhật một field, không phải mẫu cho code mới. Chỉ giữ compatibility khi tìm được consumer thực tế, có contract test và exit plan.

## Guardrail dùng cho mọi prompt

- Đọc AGENTS.md có hiệu lực trước khi làm việc, sau đó chạy git status --short --branch. Giữ nguyên mọi thay đổi có sẵn ngoài phạm vi.
- Runtime source và hướng dẫn repository hiện hành thắng template/tài liệu cũ. Nêu rõ mâu thuẫn thay vì suy diễn.
- Không reset, restore, stash, rebase, push hoặc xóa/ghi đè hàng loạt. Chỉ tạo nhánh mới đúng Branch Gate bên dưới; không tự đổi nhánh để né thay đổi có sẵn của người dùng.
- Audit và plan là read-only: không sửa source, config, migration hoặc commit.
- Không đổi route/public JSON payload, authorization, schema/migration, business formula, data scope, ownership hoặc UX nếu manifest không cho phép rõ ràng.
- Application không được phụ thuộc EF/HTTP/UI; endpoint không inject concrete Infrastructure; UI không biết persistence entity/SQL.
- Interface xuất hiện vì capability/use case thực, không phải để làm code trông SOLID.
- Server là authority cho authorization, validation, derived value, ownership, lock, concurrency, transaction và audit. UI validation chỉ hỗ trợ trải nghiệm.
- Mọi mutation phải có validation server-side, cancellation, error mapping, audit evidence và test phù hợp.
- Khi tạo hoặc refactor source, comment/XML documentation phải giải thích ý định, responsibility, invariant, side effect hoặc lý do của logic không hiển nhiên; không lặp lại câu lệnh, tạo summary chung chung, để comment lỗi thời, comment-out code, TODO vô chủ hoặc thông tin nhạy cảm. Chi tiết và checklist nằm ở 15-chuan-hoa-comment-source-code.md.
- Khi code/config được thay đổi, tuân thủ AGENTS.md: chạy kiểm tra phù hợp trước khi kết thúc, chỉ stage đúng phạm vi, tạo commit độc lập nếu work item hiện tại đã hoàn tất, và không push.
- Báo cáo chỉ nêu build/test pass khi đã chạy; phân biệt pass, fail, skip và baseline failure ngoài phạm vi.

## Branch Gate bắt buộc trước refactor

Audit, source map và plan vẫn là read-only trên nhánh hiện tại. Tuy nhiên, trước lần sửa source/config đầu tiên của một refactor, phải tạo một nhánh mới dành riêng cho work item theo branch trong manifest.

1. Xác nhận manifest có branch.base và branch.name, branch.name chưa tồn tại và khác branch.base.
2. Chạy git status --short --branch. Nếu worktree có thay đổi ngoài scope hoặc không xác định ownership, không reset/stash/chuyển nhánh; báo Branch Gate blocked và xin hướng xử lý.
3. Khi worktree sạch và base đã được phê duyệt, ghi lại base commit, rồi tạo nhánh bằng git switch --create {{branch.name}} {{branch.base}}. Không tự fetch, pull hoặc rebase.
4. Xác minh git branch --show-current trả về {{branch.name}} trước khi sửa. Không refactor trực tiếp trên main, branch base hoặc một nhánh feature cũ.
5. Nếu nhánh tên đó đã tồn tại, không tái sử dụng/ghi đè nó. Dùng tên mới đã được phê duyệt và cập nhật manifest, hoặc báo blocker.

Ngoại lệ duy nhất là AUDIT_ONLY hoặc documentation-only không sửa source/config. Mọi prompt implement, safe fix hoặc bổ sung test/source đều phải kiểm tra Branch Gate.

## Cách dùng đầu vào

Điền các placeholder có dạng {{TEN_BIEN}} trong file 00. Dán manifest đã điền cùng kết quả các bước trước vào prompt đang chạy. Có thể lưu các báo cáo vào {{ANALYSIS_ARTIFACT_ROOT}} nếu manifest cho phép cập nhật tài liệu; nếu không, giữ chúng trong hội thoại nhưng không thay đổi repository ở các bước read-only.

Mọi đường dẫn bên trong manifest phải bắt đầu từ Vnta-Blazor-2026 để prompt độc lập với máy local.
