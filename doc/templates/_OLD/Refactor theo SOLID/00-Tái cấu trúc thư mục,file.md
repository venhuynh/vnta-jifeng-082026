# Prompt — Tái cấu trúc feature theo SOLID (UI đến backend)

> Cách dùng: copy toàn bộ prompt bên dưới vào AI agent, thay các giá trị trong phần **Đầu vào**, rồi ra lệnh thực hiện. Với một màn hình mới, chỉ cần đổi `Feature`, `UI root` và mô tả nghiệp vụ; agent phải tự lần theo toàn bộ dependency từ UI đến backend.

---

```text
Bạn là senior .NET/Blazor architect và refactoring engineer. Hãy thực hiện refactor theo feature-first và SOLID cho feature được chỉ định dưới đây. Đây là yêu cầu IMPLEMENT, không chỉ phân tích hoặc đề xuất.

## Đầu vào bắt buộc

- Feature group: `PhuCap`
- Feature name: `PhuCapTrachNhiemCapBac`
- Tên hiển thị/nghiệp vụ: `Cấp bậc phụ cấp trách nhiệm`
- UI root hiện tại: `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/PhuCap/PhuCapTrachNhiemCapBac`
- UI route cần giữ nguyên: `/payroll/responsibility-allowances/grades`
- Mô tả ngắn hành vi nghiệp vụ: Tải danh sách cấp bậc phụ cấp trách nhiệm của kỳ mặc định, tìm kiếm theo mã/tên/ghi chú, phân trang và chọn dòng; cho phép thêm mới hoặc sửa mã bậc, tên bậc, tiền chuẩn, thứ tự hiển thị, trạng thái sử dụng và ghi chú; ngừng dùng cấp bậc đang hoạt động sau khi xác nhận; xuất toàn bộ hoặc các dòng đã chọn ra Excel/PDF.
- Phạm vi thay đổi nghiệp vụ được phép: Chỉ refactor cấu trúc; giữ nguyên UX, route, authorization, validation, audit, optimistic concurrency, trạng thái sử dụng/ngừng dùng và các HTTP contract hiện có.
- Các file hoặc thay đổi đang có cần tuyệt đối giữ nguyên: Giữ nguyên toàn bộ behavior của `PhuCapTrachNhiemCapBac.razor`, `PhuCapTrachNhiemCapBac.razor.cs`, `PhuCapTrachNhiemCapBacEditForm.razor`, các state tải/chọn dòng và các luồng thêm, sửa, ngừng dùng, tìm kiếm, phân trang, xuất Excel/PDF.

## Mục tiêu bắt buộc

Đưa toàn bộ source code **chỉ thuộc feature này** về cấu trúc feature-first đồng nhất từ UI đến backend. Giữ hành vi, quyền, route và HTTP contract hiện có trừ khi phần “phạm vi thay đổi nghiệp vụ” cho phép khác.

```text
src/Vnta.HRM2026/
├─ Vnta.Hrm.Web.Client/
│  ├─ Components/[Feature group]/[Feature name]/
│  │  ├─ [Feature name].razor                 # page host: route + compose UI
│  │  ├─ [Feature name].razor.cs              # coordinator mỏng, không ôm toàn bộ UI workflow
│  │  ├─ Sections/                             # toolbar, summary, grid, pager...
│  │  ├─ Dialogs/                              # popup/dialog riêng theo use case
│  │  ├─ State/                                # page/filter/selection state
│  │  ├─ Models/                               # view/edit models chỉ dành cho UI
│  │  └─ Export/                               # export coordinator/model nếu feature có export
│  └─ Services/
│     ├─ Api/[Feature group]/[Feature name]/
│     └─ DataProviders/[Feature group]/[Feature name]/
│
├─ Vnta.Hrm.Application/
│  └─ [Feature group]/[Feature name]/
│     ├─ Contracts/                            # capability interfaces hẹp
│     ├─ Queries/                              # filters, query DTOs, page/summary DTOs
│     ├─ Commands/                             # command requests/results
│     ├─ Policies/                             # policy/calculator/domain-rule abstraction
│     └─ Exceptions/
│
├─ Vnta.Hrm.Web/
│  └─ Endpoints/[Feature group]/[Feature name]/
│     ├─ [Feature]EndpointMappings.cs
│     ├─ [Feature]QueryEndpoints.cs
│     └─ [Feature]CommandEndpoints.cs
│
├─ Vnta.Hrm.Infrastructure/
│  └─ [Feature group]/[Feature name]/
│     ├─ Persistence/                          # EF entity/configuration chỉ của feature
│     ├─ Queries/                              # database read service/query projection
│     ├─ Commands/                             # một service/handler cho mỗi command capability
│     ├─ Policies/                             # policy adapter khi policy cần infrastructure
│     └─ DependencyInjection/                  # DI registration của feature
│
├─ Vnta.Hrm.Infrastructure.Tests/
│  └─ [Feature group]/[Feature name]/
│     ├─ Policies/
│     ├─ Queries/
│     └─ Commands/
│
└─ Vnta.Hrm.Web.Tests/
   └─ Endpoints/[Feature group]/[Feature name]/
```

Không bắt buộc tạo thư mục rỗng. Chỉ tạo thư mục/file có source hoặc test thật sự cần thiết. Nếu kiến trúc repo hiện hữu có convention tương đương nhưng tên khác, ưu tiên convention nhất quán của repo và giải thích ngắn khác biệt.

## Quy trình bắt buộc

### 1. Khảo sát trước khi sửa

1. Đọc đầy đủ `AGENTS.md` áp dụng cho workspace và các hướng dẫn dự án trước khi thao tác.
2. Kiểm tra `git status --short`; coi mọi thay đổi sẵn có là của người dùng. Không reset, checkout, xóa hoặc ghi đè chúng.
3. Dùng `rg` để lập dependency map đầy đủ cho feature:
   - route/page/component/popup/UI model;
   - handlers của từng nút hoặc event UI;
   - data provider, HTTP service, endpoint URL/method;
   - application interface, DTO, command/query, exception, policy;
   - DI registrations ở client/server/infrastructure;
   - EF entity, configuration, migration reference;
   - tests hiện có và consumer ngoài feature.
4. Trước khi di chuyển một type, tìm toàn bộ usages. Chỉ di chuyển type nếu nó thực sự thuộc feature; type dùng chung phải ở common/shared location hiện hữu.
5. Trình bày ngắn dependency map và danh sách file dự kiến di chuyển/tách. Sau đó triển khai luôn, trừ khi gặp blocker cần quyết định nghiệp vụ hoặc quyền hạn mới.

### 2. Quy tắc thiết kế SOLID

- **Single Responsibility**
  - Page host chỉ compose UI, giữ route/authorization và điều phối state ở mức cao.
  - Toolbar, grid, summary, pager, export, từng dialog và monthly-work/related popup là component/coordinator riêng.
  - Không để một database service vừa read/query, export, refresh/recalculate, manual adjustment, lock, transaction, audit và policy rule.
- **Open/Closed**
  - Quy tắc tính/đủ điều kiện phải là policy/calculator rõ ràng, test độc lập.
  - Không nhét rule nghiệp vụ mới vào query/persistence service nếu có thể inject policy.
- **Interface Segregation**
  - Endpoint và UI use case phụ thuộc interface hẹp: `Read`, `Refresh/Recalculate`, `ManualAdjustment`, `Lock`, `Export` nếu cần.
  - Composite interface cũ chỉ giữ tạm để tương thích; đánh dấu obsolete hoặc lên kế hoạch xóa sau khi không còn consumer.
- **Dependency Inversion**
  - UI phụ thuộc provider/contract, không truy cập `DbContext` hay repository.
  - Endpoint phụ thuộc application contract, không phụ thuộc concrete infrastructure class.
  - Infrastructure implement application contracts và chứa EF/Npgsql details.
- Tách query khỏi command ở mức phù hợp; không đưa CQRS framework mới vào nếu repo không đang dùng.

### 3. Quy tắc an toàn và tương thích

1. Giữ nguyên route, authorization policy, endpoint URL, HTTP verb, JSON property name và response shape trong đợt refactor này.
2. Giữ nguyên semantics của cancellation, loading state, optimistic concurrency, audit actor và error mapping (`400/409/...`).
3. Không sửa migration lịch sử. Khi phải di chuyển EF entity/configuration, chỉ cập nhật namespace/usings/model configuration sao cho schema không đổi; tạo migration mới chỉ khi schema thực sự đổi và đã được cho phép.
4. Không đổi nghiệp vụ ngầm. Nếu phát hiện mâu thuẫn như “UI sửa A nhưng backend tính bằng B”, hãy:
   - ghi rõ bằng chứng file/dòng;
   - bổ sung characterization test nếu làm được;
   - giữ behavior hiện tại trong refactor;
   - dừng trước semantic change và yêu cầu quyết định nếu không có quyền thay đổi nghiệp vụ.
5. Không tạo compatibility alias/wrapper mới nếu không có consumer cũ. Nếu phải giữ alias để tránh breaking change, đánh dấu `[Obsolete]`, giới hạn phạm vi và nêu kế hoạch xóa.
6. Không để file source trùng lặp sau khi di chuyển; cập nhật namespace, registrations và usages để chỉ còn canonical implementation.
7. Ưu tiên di chuyển bằng thao tác giữ lịch sử Git khi khả dụng; chỉnh sửa nội dung nhỏ, có chủ đích. Không dùng thao tác phá hủy worktree.

### 4. Cách triển khai

Thực hiện theo các commit logic hoặc các nhóm thay đổi dễ review sau đây (không tự commit nếu người dùng không yêu cầu):

1. Tạo canonical folders và di chuyển source hiện có, cập nhật namespaces/usings/DI nhưng chưa đổi behavior.
2. Tách Application contracts, query DTOs, command DTOs, policy và exception theo thư mục đích.
3. Tách Infrastructure thành read service, command services, policy và persistence configuration; bảo toàn transaction/concurrency.
4. Tách endpoint mapping, query endpoint và command endpoint; mọi endpoint phải dùng narrow contract.
5. Tách client HTTP service/data provider vào feature path; không để component gọi HTTP trực tiếp.
6. Làm mỏng page host; tách Sections, Dialogs, State và Export mà không thay UI/UX đã có.
7. Di chuyển/bổ sung tests theo feature path, sau đó chỉ dọn source/alias cũ khi `rg` xác nhận không còn usage.

Không ép mọi partial class thành child component nếu việc đó làm phá binding hoặc tăng rủi ro quá mức. Trong trường hợp đó, giữ partial class cùng namespace nhưng tách theo use case rõ ràng và ghi backlog componentization còn lại.

### 5. Definition of Done

Chỉ báo hoàn thành khi tất cả điều kiện sau đúng:

- Cấu trúc canonical được tạo và feature code không còn nằm rải rác ở các vị trí cũ, trừ common/shared code có lý do rõ ràng.
- Mỗi nút/command UI đã lần được tới đúng provider → endpoint → application contract → infrastructure implementation.
- Page host và database service không còn là “god class”; trách nhiệm đã được tách theo các use case có ý nghĩa.
- DI ở client, web và infrastructure đã cập nhật; không còn registration trỏ tới class cũ không dùng.
- Không còn duplicate implementation hoặc dead alias chưa có lý do.
- Build các project bị ảnh hưởng thành công.
- Chạy toàn bộ test feature liên quan; bổ sung test cho policy, command, concurrency và endpoint contract khi thiếu.
- Nếu có test/build lỗi không do thay đổi của bạn, báo rõ lệnh, lỗi và nguyên nhân dự kiến; không che giấu hoặc bỏ qua.

## Báo cáo cuối cùng bắt buộc

Trả lời bằng tiếng Việt, ngắn gọn nhưng có bằng chứng:

1. Kết quả và những thay đổi đã thực hiện.
2. Bảng `đường dẫn cũ → đường dẫn mới` cho mọi file di chuyển/tách chính.
3. Luồng UI → backend sau refactor, liệt kê endpoint/method cho từng command.
4. Các điểm SOLID đã cải thiện và phần còn lại chưa làm.
5. Kết quả build/test và các lệnh đã chạy.
6. Blocker hoặc quyết định nghiệp vụ còn cần người dùng xác nhận.

Hãy bắt đầu bằng khảo sát repository rồi thực hiện refactor cho feature được cung cấp.
```
