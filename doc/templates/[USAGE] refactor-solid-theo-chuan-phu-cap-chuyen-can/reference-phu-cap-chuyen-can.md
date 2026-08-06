# Chuẩn tham chiếu: Phụ cấp chuyên cần

## Phạm vi của tài liệu tham chiếu

Phụ cấp chuyên cần là golden reference về cách phân lớp và quality gate tại revision được ghi trong Feature Refactor Manifest. Tài liệu này không yêu cầu feature đích phải có cùng UI action, model, API route, công thức, record hoặc folder đầy đủ. Action không tồn tại ở feature đích phải được đánh dấu N/A, không tự tạo để giống mẫu.

## Boundary được tham chiếu

    Blazor page, sections và dialogs
      -> capability DataProvider
      -> HTTP implementation của Application contracts
      -> authorized Web endpoint và audit scope
      -> focused Application contract/policy
      -> Infrastructure use case và EF adapter
      -> feature-owned detail snapshot + summary projection

Các thư mục nguồn tiêu biểu:

- UI: Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapChuyenCan với Commands, Dialogs, Export, Models, Presentation, Sections và State.
- Client boundary: Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Services/DataProviders/PhuCap/PhuCapChuyenCan và Services/Api/PhuCap/PhuCapChuyenCan.
- Application: Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Application/PhuCap/PhuCapChuyenCan với Commands, Contracts, Exceptions, Policies và Queries.
- Web: Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Web/Endpoints/PhuCap/PhuCapChuyenCan với EndpointMappings, QueryEndpoints, CommandEndpoints và EndpointExecution.
- Infrastructure: Vnta-Blazor-2026/src/Vnta.HRM2026/Vnta.Hrm.Infrastructure/PhuCap/PhuCapChuyenCan với Commands, Queries, Policies, Persistence và DependencyInjection.
- Tài liệu hành vi hiện có: Vnta-Blazor-2026/doc/screens/PhuCap/PhuCapChuyenCan.

## Pattern áp dụng được

### UI và client

- Page host điều phối lifecycle/state; Sections và Dialogs nhận parameter/EventCallback thay vì giữ nghiệp vụ/persistence riêng.
- State async có cancellation/dispose và bảo vệ stale response; filter construction, mapping và presentation tách khỏi page nếu có trách nhiệm riêng.
- UI inject capability nhỏ nhất cần dùng: Read, Export, Refresh, ManualAdjustment, Lock. Composite provider chỉ để tương thích consumer/test cũ.
- Client provider map DTO transport sang view record và mở audit scope đúng runtime; không tính công thức nghiệp vụ hoặc tự quyết authorization.

### Application, Web và Infrastructure

- Contract tách theo use case, request/DTO không lộ entity EF. Query/read, export và mutation có responsibility riêng.
- Pure policy chứa calculation, period, validation logic; UI lấy rule metadata từ server thay vì lặp literal threshold/amount/config.
- Endpoint group yêu cầu authorization; endpoint chỉ bind/validate basic, tạo audit context server-side, gọi contract và map failure.
- Infrastructure tách query projection, export và command. Query server-side filter/page/order; command owns transaction, final-state validation và persistence.
- DI feature-local đăng ký interface/implementation, giúp composition root không biết chi tiết từng feature.

### Ownership, atomicity và audit

- Detail Chuyên cần là canonical writer cho giá trị phụ cấp chuyên cần; Payroll Allowance Summary chỉ nhận projection và không expose field này trong manual update generic.
- Cập nhật actual/standard workday chuẩn là một atomic command có concurrency token, kiểm tra detail + summary lock, transaction và reload result.
- Audit tracked change phải có policy cho detail và summary/property nhạy cảm; raw/bulk write phải dùng cơ chế audited mutation phù hợp.

## Những phần không copy nguyên xi

- Endpoint điều chỉnh một field và composite provider cũ chỉ được giữ để tương thích. Code mới ưu tiên command theo aggregate/invariant, không tạo endpoint single-field chỉ vì mẫu có.
- Batch lock cũ có thể hỗ trợ payload chưa versioned. Mutation mới phải truyền concurrency token nếu UI đang chọn record có phiên bản.
- Không copy literal period, threshold, money amount, action inventory, record type, route prefix, CSS hay popup markup.
- Không copy máy móc câu chữ comment/XML documentation từ source tham chiếu. Dùng architecture/invariant đã được chứng minh để viết comment đúng với feature đích theo bước 15.
- Phụ cấp chuyên cần còn có một số compatibility và presentation validation tồn tại vì lịch sử. Mọi feature mới phải ưu tiên single source of truth hơn là sao chép duplicate rule.

## Câu kiểm tra trước khi dùng làm mẫu

Hãy trả lời cho từng pattern: feature đích có use case tương ứng không; có consumer/contract hiện hữu nào buộc compatibility không; canonical writer là ai; invariants nào phải atomic; và test nào sẽ chứng minh pattern đó hoạt động? Nếu một câu không có evidence, tiếp tục discovery thay vì copy code.
