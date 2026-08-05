# HRM Refactor Standard

Tài liệu này là chuẩn cấp dự án để refactor và mở rộng `src/Vnta.HRM2026`
theo cùng một hướng boundary, contract và ownership dữ liệu.

Tài liệu này không thay thế:

- `doc/rules/`
- `doc/checklists/`
- `doc/project/hrm-list-screen-blueprint.md`

Nó đóng vai trò "nguồn sự thật cấp điều phối" để trả lời các câu hỏi:

- logic nào được đặt ở UI
- logic nào phải nằm ở server
- khi nào cần endpoint HTTP riêng
- khi nào một màn đang ở pattern chuyển tiếp và cần ghi nợ kỹ thuật
- folder và file nên được tổ chức theo context key nào

## 1. Mục đích và phạm vi

Áp dụng cho:

- màn CRUD danh mục
- màn vận hành attendance
- màn payroll snapshot
- đợt refactor boundary giữa UI, application service và database

Không áp dụng để thay thế đặc tả nghiệp vụ chi tiết của từng feature.

Khi có xung đột, ưu tiên theo thứ tự:

1. yêu cầu nghiệp vụ mới nhất
2. rule trong `doc/rules/`
3. tài liệu chuẩn này
4. template và checklist

## 2. Pattern đích cần hướng tới

Pattern ưu tiên cho màn nghiệp vụ mới:

```text
UI component
  -> data provider hoặc typed client
  -> HTTP endpoint hoặc API boundary
  -> application service
  -> infrastructure persistence/integration
  -> PostgreSQL
```

Ý nghĩa của từng tầng:

- UI:
  - giữ markup, view-state, loading, selection, popup, toast
  - không kết luận business rule cuối cùng
- Data provider hoặc typed client:
  - map model UI sang request/response contract
  - gom xử lý giao tiếp với endpoint
- HTTP endpoint:
  - là boundary contract rõ ràng giữa UI và server
  - nhận request/response có tên nghiệp vụ rõ nghĩa
- Application service:
  - chứa business rule, validation cuối cùng, orchestration, command
- Infrastructure:
  - sở hữu EF Core, SQL, migration, transaction, integration ngoài

## 3. Pattern chuyển tiếp được phép

Repo hiện tại vẫn có màn `InteractiveServer` chưa tách boundary HTTP riêng.
Pattern này được phép trong giai đoạn chuyển tiếp nếu tất cả điều kiện sau
được đáp ứng:

```text
UI component
  -> data provider
  -> application service abstraction
  -> infrastructure service
  -> PostgreSQL
```

Điều kiện bắt buộc:

- UI vẫn không được inject `ApplicationDbContext`
- UI vẫn chỉ làm việc qua provider hoặc service abstraction
- divergence này phải được ghi rõ trong screen đọc
- phải ghi rõ nợ kỹ thuật và hướng hội tụ về pattern đích

Kết luận:

- pattern chuyển tiếp là chấp nhận được
- pattern đích vẫn là `UI -> endpoint -> service -> database`

## 4. Boundary bắt buộc giữa UI và database

### UI layer

UI được phép:

- quản lý `SearchText`, `SelectedDataItems`, popup visibility, loading state
- normalize model UI mức nhẹ để phục vụ editor
- điều phối export, confirm dialog, retry

UI không được phép:

- query EF Core trực tiếp
- viết SQL
- chạy migration hoặc schema guard
- kết luận validation cuối cùng rồi coi đó là source of truth
- dùng chung persistence entity làm edit model sống lâu dài

### Application layer

Application service phải sở hữu:

- validation cuối cùng
- business rule cốt lõi
- duplicate check
- command nghiệp vụ riêng
- xác định source of truth
- orchestration save pipeline

Save pipeline ưu tiên:

```text
normalize request
-> đồng bộ state phụ thuộc
-> validate
-> transaction/persistence
-> map response DTO
```

### Infrastructure layer

Infrastructure phải sở hữu:

- `ApplicationDbContext`
- EF configuration
- migration
- transaction thực thi
- query/persistence tới PostgreSQL
- kết nối external source như attendance gateway

## 5. Contract và model

Nguyên tắc bắt buộc:

- không trả EF entity trực tiếp về UI
- request/response phải có tên nghiệp vụ rõ ràng
- view model UI và persistence row model phải tách riêng
- model UI chỉ được giữ logic preview nhẹ, không giữ logic nghiệp vụ cuối cùng
- `Xxx` trong tên file và class ưu tiên là `Context key` bằng tiếng Việt không dấu
- tên tiếng Anh chỉ đóng vai trò `Technical alias` nếu phải map với schema hoặc integration

Mẫu đặt tên khuyến nghị:

- filter: `XxxFilter`
- list item DTO: `XxxListItemDto`
- command request: `CreateXxxRequest`, `UpdateXxxRequest`, `UpsertXxxRequest`
- command service: `IXxxService`, `IXxxRefreshService`, `IXxxWorkflowService`
- typed client: `HttpXxxService`
- provider UI: `XxxDataProvider`

Folder và naming xuyên layer được chốt chi tiết tại:

- `doc/project/feature-folder-standard.md`
- `doc/project/infrastructure-feature-folder-map.md`
- `doc/project/cross-project-feature-folder-refactor-plan.md`

## 6. Search, filter, paging và command nghiệp vụ

### Search/filter/paging

Chuẩn đích:

- search, filter, paging và data shaping ưu tiên xử lý server-side
- grid search client-side chỉ nên dùng cho tập dữ liệu nhỏ hoặc màn tạm thời
- nếu còn dùng search client-side, phải ghi rõ đây là nợ kỹ thuật trong screen đọc

### Command nghiệp vụ riêng

Không nhét các action sau vào CRUD chung:

- `refresh`
- `sync`
- `sync-summary`
- `lock`
- `unlock`
- `approve`
- `retry`
- `import`

Mỗi command phải có:

- contract riêng
- validation riêng
- feedback riêng
- mô tả source of truth liên quan

## 7. Transaction, audit và concurrency

### Transaction

Bắt buộc có transaction rõ ràng khi:

- một command cập nhật nhiều bảng
- đồng bộ từ nhiều nguồn
- save một record đồng thời cần cập nhật summary hay snapshot khác

### Audit

Bảng runtime nghiệp vụ nên có owner rõ ràng cho:

- `CreatedAtUtc`
- `UpdatedAtUtc`
- `CreatedBy`
- `UpdatedBy`
- `DeletedAtUtc` nếu có soft delete

### Concurrency

Khi màn có update workflow thật sự, phải xác định rõ:

- có cần `RowVersion` hay optimistic concurrency token không
- nếu conflict xảy ra thì UI báo gì và user xử lý thế nào

Nếu chưa có update workflow, tài liệu màn hình phải ghi rõ gate này chưa đóng.

## 8. Schema, migration và database guard

Chuẩn đích:

- schema ownership nằm ở `Infrastructure`
- migration được tạo qua EF migration và được review
- unique constraint quan trọng nên có ở database, không chỉ chặn ở service level
- không chạy schema guard/database guard trong mỗi request nếu không thật sự cần

Nếu tạm thời cần schema guard runtime:

- phải ghi lý do vận hành rõ ràng
- phải ghi kế hoạch loại bỏ
- phải xem đó là nợ kỹ thuật, không xem là pattern chuẩn

## 9. Cấu trúc màn hình Blazor khuyến nghị

Với list screen chuẩn:

- dùng `doc/project/hrm-list-screen-blueprint.md`
- tách tối thiểu:
  - `Screen.razor`
  - `Screen.razor.cs`
  - `Screen.razor.css`
  - `ScreenEditForm.razor` hoặc popup độc lập

Nguyên tắc:

- state sống ở `.razor.cs`
- popup phức tạp là component riêng
- validation hiện trong form, không chỉ hiện qua toast
- toolbar action theo đúng thứ tự chung của repo

## 10. Hai pilot đang định nghĩa chuẩn

### Pilot A - `NhanVien`

Tài liệu tham chiếu:

- `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`
- `doc/checklists/operational-list-data-processing-standard.md`

Giá trị pilot:

- là example chuẩn đầu tiên cho `Operational List Page` trong HRM
- cho thấy cách hội tụ flow `UI -> provider -> typed client -> endpoint -> service -> database`
- cho thấy cách tách `summary query`, `list query` và command `Làm mới` khỏi CRUD
- cho thấy cách gom `search`, `summary badge`, `refresh`, `save` về một `ReloadAsync()` chung

Kết luận kiến trúc:

- hợp lệ và đang được dùng làm màn tham chiếu cho các list screen vận hành mới
- cần tiếp tục theo dõi các gap:
  - schema guard còn chạy runtime
  - native `RowVersion` chưa được dùng; hiện mới khóa optimistic concurrency bằng `UpdatedAtUtc`
  - cần tiếp tục rollout cùng pattern sang các màn vận hành khác

### Pilot B - `PhuCapCom`

Tài liệu tham chiếu:

- `doc/screens/phu-cap/phu-cap-com.md`
- `doc/sprints/sprint-016-phu-cap-com-implementation/sprint-plan.md`

Giá trị pilot:

- là example gần hơn với pattern đích
- UI đi qua provider -> HTTP endpoint -> application service -> database
- tách được bảng snapshot riêng `payroll_meal_allowance_records`

Kết luận kiến trúc:

- phù hợp để làm mẫu cho payroll snapshot screens
- vẫn còn gate mở:
  - `refresh` từ attendance
  - `sync-summary`
  - final business rules payroll

## 11. Playbook refactor từng màn

Khi refactor một màn hình, thứ tự khuyến nghị:

1. Viết hoặc cập nhật screen spec.
2. Điền `doc/templates/screen-implementation-template.md`.
3. Chốt `bounded context`, `context key` và folder map xuyên layer.
   - `context key` ưu tiên tiếng Việt không dấu theo cách người dùng và team đang gọi feature.
4. Vẽ rõ boundary hiện trạng và boundary đích.
5. Chốt source of truth và command nghiệp vụ riêng.
6. Tách request/response/DTO nếu màn còn dùng model mơ hồ.
7. Đưa search/filter/paging lên server nếu tập dữ liệu có thể lớn.
8. Xác định transaction, audit và concurrency.
9. Cập nhật checklist, implementation log và sprint đọc.

## 12. Định nghĩa đạt chuẩn

Một màn được coi là đạt chuẩn refactor khi:

- boundary UI và server được mô tả rõ
- không inject `ApplicationDbContext` vào Razor component
- không trả EF entity trực tiếp về UI
- command nghiệp vụ riêng đã tách khỏi CRUD
- validation cuối cùng nằm ở server
- transaction/concurrency đã được xác định rõ nếu workflow cần
- nợ kỹ thuật còn lại đã được ghi trong screen đọc và implementation log

## 13. Tài liệu cần đọc cùng

- `doc/project/architecture.md`
- `doc/project/refactor-roadmap.md`
- `doc/project/feature-folder-standard.md`
- `doc/project/hrm-list-screen-blueprint.md`
- `doc/checklists/screen-implementation-principles.md`
- `doc/checklists/ui-screen-checklist.md`
- `doc/checklists/operational-list-screen-checklist.md`
- `doc/checklists/operational-list-data-processing-standard.md`
- `doc/checklists/done-checklist.md`
- `doc/templates/screen-implementation-template.md`
- `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`

## 14. Quy tắc áp dụng từ sau tài liệu này

Từ branch này trở đi:

- màn mới phải có screen implementation đọc nếu là màn quan trọng hoặc có persistence
- nếu màn không theo pattern đích, phải ghi rõ đây là pattern chuyển tiếp
- refactor PR không được chỉ sửa UI mà bỏ trống boundary, validation và source of truth

Tài liệu này là baseline `v1` cho chuẩn refactor. Nếu thực tế repo thay đổi,
cập nhật tài liệu này trước khi nhân rộng pattern mới.


