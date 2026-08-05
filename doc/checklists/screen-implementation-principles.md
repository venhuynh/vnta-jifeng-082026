# Checklist Nguyên Tắc Triển Khai Màn Hình Mới

Áp dụng bắt buộc khi tạo mới hoặc refactor màn hình nghiệp vụ trong
`src/Vnta.HRM2026`.

Tài liệu này là checklist gốc về nguyên tắc triển khai. Tài liệu
`ui-screen-checklist.md` là checklist ngắn để thao tác theo từng màn.

Màn tham chiếu chuẩn hiện tại của repo cho kiểu `Operational List Page` là:

- [NhanVien.razor](../../src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/NhanVien/NhanVien.razor)
- [NhanVien.razor.cs](../../src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/NhanVien/NhanVien.razor.cs)
- `doc/checklists/operational-list-data-processing-standard.md`

Khi có xung đột, ưu tiên theo thứ tự:

1. screen spec hoặc sprint spec mới nhất
2. rule trong `doc/rules/`
3. checklist nguyên tắc này
4. checklist thao tác ngắn của từng màn

## 1. Định hướng trước khi code

- [ ] Đã xác định đây là màn CRUD danh mục, màn vận hành, màn tổng hợp hay màn dashboard, và đã chọn đúng pattern UI.
- [ ] Đã chốt `bounded context` và `context key` bằng tiếng Việt không dấu để dùng xuyên suốt cho folder, file và class liên quan đến feature.
- [ ] Đã vẽ rõ luồng giao tiếp dữ liệu của màn: `UI -> provider/typed client -> endpoint -> application/persistence service -> database`.
- [ ] Đã xác định rõ phần nào là view-state của UI, phần nào là business rule bắt buộc phía server.
- [ ] Nếu đây là màn danh sách nghiệp vụ, đã đối chiếu thêm `doc/checklists/operational-list-data-processing-standard.md` để bám đúng flow `firstRender -> ReloadAsync -> provider -> API -> service -> database` như `NhanVien`.
- [ ] Đã đối chiếu `doc/project/architecture.md`, `doc/rules/source-boundary-rules.md` và `doc/project/hrm-list-screen-blueprint.md`.
- [ ] Đã đối chiếu thêm `doc/project/feature-folder-standard.md` nếu màn này mở folder mới hoặc refactor naming lớn.
- [ ] Nếu màn dùng DevExpress component mới hoặc pattern mới, đã đối chiếu thêm với MCP DevExpress `dxdocs26_1` theo cấu hình `src/Vnta.HRM2026/.mcp.json`, hoặc tối thiểu đối chiếu với tài liệu DevExpress chính thức từ cùng nhóm API.

## 2. Boundary bắt buộc giữa UI và database

- [ ] UI không inject `ApplicationDbContext`, không truy vấn EF Core, SQL hoặc migration trực tiếp.
- [ ] UI chỉ được gọi qua data provider, typed client hoặc service abstraction đã được phê duyệt.
- [ ] Các file liên quan của feature được tổ chức theo cùng `context key`, không để UI, endpoint, service và row model mang các tên lệch ngữ cảnh.
- [ ] Nếu cần giữ tên tiếng Anh do schema hoặc integration cũ, đã ghi rõ nó là `technical alias`, không xem nó là tên quản lý chính của feature.
- [ ] `*.razor` và `*.razor.cs` chỉ giữ markup, view-state, orchestration UI, loading, selection, dialog, toast và validation nhẹ.
- [ ] Business rule cốt lõi, validation cuối cùng, tính toán nghiệp vụ, lock state, transaction và persistence nằm ở phía server.
- [ ] `Infrastructure` là nơi sở hữu EF Core, SQL, migration, schema và kết nối database.
- [ ] Không trả EF entity trực tiếp về UI; UI chỉ nhận DTO, request, response hoặc view model riêng.
- [ ] UI model không được dùng chung trực tiếp với row model persistence nếu màn còn sống lâu dài.

## 3. Hợp đồng dữ liệu và nghiệp vụ

- [ ] Search, filter, paging và data shaping ưu tiên xử lý server-side.
- [ ] Với màn danh sách nghiệp vụ có summary badge, search và grid cùng tồn tại, summary và list phải dùng chung một nguồn filter logic, không để summary đếm theo một tập dữ liệu còn grid hiển thị theo tập khác.
- [ ] Màn có một entry point reload chính như `ReloadAsync()` để gom mọi trigger `firstRender`, `search`, `summary badge`, `refresh`, `save success`.
- [ ] Save pipeline theo thứ tự: normalize model -> đồng bộ state phụ thuộc -> validate -> mới persistence.
- [ ] Các action nghiệp vụ như `refresh`, `sync`, `sync-summary`, `lock`, `unlock`, `import`, `retry`, `approve` được tách thành command riêng, không nhét chung vào CRUD.
- [ ] Nếu màn có tổng hợp từ nhiều bảng hoặc nhiều nguồn, đã xác định rõ ai là source of truth và ai là snapshot/runtime store.
- [ ] API contract có tên rõ nghĩa nghiệp vụ, không dùng payload mơ hồ hoặc `object` generic cho business action.

## 4. Triển khai DevExpress ở mức màn hình

- [ ] Toolbar tổng của màn dùng `DxToolbar` và action được sắp theo thứ tự nhất quán với repo.
- [ ] Với `Operational List Page`, toolbar chủ yếu giữ action nghiệp vụ; search và summary badge ưu tiên đặt trong header của data surface như màn `NhanVien`, không ép mọi thứ nhét vào toolbar.
- [ ] Loading vùng dữ liệu ưu tiên đi qua `HrmLoadingPanel`; nếu cần raw `DxLoadingPanel`, đã xác định rõ target và cách bật tắt visibility.
- [ ] Nếu dùng `DxGrid`, đã khai báo `KeyFieldName` ổn định cho selection/focus.
- [ ] Nếu dùng `DxSearchBox` thay cho `ShowSearchBox`, đã dùng `BindValueMode.OnDelayedInput` với `InputDelay` rõ ràng và normalize text trước khi reload.
- [ ] Nếu dùng popup edit của `DxGrid`, đã khai báo rõ `EditMode`, `EditFormTemplate`, `PopupEditFormCssClass`, `PopupEditFormHeaderText` và footer `Lưu/Hủy` tiếng Việt.
- [ ] Nếu dùng column chooser, đã gọi `ShowColumnChooser(...)` có chủ đích, có target rõ ràng và không để lộ các cột không nên cho người dùng tự bật.
- [ ] Validation của editor DevExpress nằm trong `EditForm`/DataAnnotations pipeline, không dùng toast thay cho validation field.
- [ ] State của selection, focused row, popup, drawer, loading và error sống ở `.razor.cs`, không viết expression dài trong Razor markup.

## 5. Quy tắc form và validation

- [ ] Mỗi workflow create/update thật sự có component form riêng như `*EditForm.razor`.
- [ ] Form hiển thị `ValidationMessage` gần field hoặc `ValidationSummary` trong body form.
- [ ] Validation message là tiếng Việt, ngắn, đúng ngữ cảnh nghiệp vụ.
- [ ] Save handler chặn persistence khi validation fail và set cancel rõ ràng nếu đang dùng event grid/tree list.
- [ ] Editor read-only không tham gia validation đã được tắt validation nếu cần.
- [ ] Toast chỉ là kênh bổ sung; lỗi validation chính phải hiện trong form.

## 6. Đa nguồn dữ liệu, transaction và concurrency

- [ ] Multi-table update hoặc sync từ nhiều nguồn có transaction rõ ràng.
- [ ] Đã xác định có cần `RowVersion` hoặc cơ chế concurrency tương đương để tránh ghi đè khi nhiều người sửa cùng lúc.
- [ ] Nếu chưa dùng `RowVersion`, đã có gate tạm rõ ràng kiểu optimistic concurrency theo `UpdatedAtUtc` hoặc field tương đương như màn `NhanVien`.
- [ ] Lock state của bản ghi, kỳ lương, snapshot hoặc tổng hợp nếu có đã được đặt ở server, không chỉ tin vào UI disable button.
- [ ] Các trường audit như `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy` hoặc actor tương đương đã có owner rõ ràng.
- [ ] Không chạy schema guard/database guard trong mỗi request nếu không có lý do vận hành thật sự rõ ràng.

## 7. Anti-pattern cấm áp dụng

- [ ] Không inject `ApplicationDbContext` vào Razor component.
- [ ] Không để `.razor.cs` tự tính kết quả nghiệp vụ cuối cùng rồi coi đó là nguồn sự thật.
- [ ] Không để UI biết quá sâu về tên bảng, tên cột, migration hay SQL shape.
- [ ] Không dùng chung một entity cho cả EF persistence và edit model UI.
- [ ] Không copy nguyên sample code DevExpress vào production mà không đưa qua boundary và naming của HRM.
- [ ] Không trộn CRUD màn hình với long-running business commands mà không tách contract riêng.

## 8. Tài liệu và verification

- [ ] Nếu màn tạo ra pattern lặp lại, đã cập nhật thêm rule hoặc checklist liên quan, không chỉ sửa một màn rồi bỏ đó.
- [ ] Đã cập nhật `doc/implementation-log/yyyyMMdd.md` trong cùng lượt thay đổi.
- [ ] Nếu màn nằm trong sprint đang mở, đã cập nhật sprint đọc liên quan.
- [ ] Đã ghi rõ cách kiểm chứng: đọc source, build, smoke test, test tay, hay lý do chưa chạy.
- [ ] Đã ghi rõ gate còn mở, assumption và rủi rõ còn lại nếu chưa khôi phục hết.

## 9. Cách dùng cùng các checklist khác

- Trình tự khuyến nghị:
  1. đọc tài liệu này trước
  2. nếu là màn mới hoặc refactor lớn, mở từ `doc/templates/screen-implementation-template.md`
  3. dùng `ui-screen-checklist.md` để mở màn
  4. dùng `ui-state-checklist.md` để rà soát state
  5. dùng `done-checklist.md` trước khi báo hoàn tất

Tài liệu này không thay thế:

- `doc/rules/blazor-devexpress-rules.md`
- `doc/rules/grid-rules.md`
- `doc/rules/edit-form-validation-rules.md`
- `doc/rules/database-rules.md`
- `doc/checklists/operational-list-data-processing-standard.md`

Nó đóng vai trò checklist gốc để chắc rằng mỗi màn mới đều bám cùng một bộ nguyên tắc triển khai.

## 10. Nguồn tham chiếu ưu tiên

### Tài liệu nội bộ

- `doc/project/architecture.md`
- `doc/project/hrm-list-screen-blueprint.md`
- `doc/rules/source-boundary-rules.md`
- `doc/rules/blazor-devexpress-rules.md`
- `doc/rules/grid-rules.md`
- `doc/rules/edit-form-validation-rules.md`
- `doc/rules/database-rules.md`

### DevExpress chính thức

Repo này chuẩn hóa tra cứu DevExpress qua MCP `dxdocs26_1`. Khi MCP tool không được expose trong runtime agent, đối chiếu lại bằng các topic chính thức sau:

- `DxToolbar`: https://docs.devexpress.com/Blazor/DevExpress.Blazor.DxToolbar
- `DxLoadingPanel`: https://docs.devexpress.com/Blazor/DevExpress.Blazor.DxLoadingPanel
- `DxGrid` edit forms: https://docs.devexpress.com/Blazor/404757/components/grid/editing-and-validation/edit-modes/edit-forms
- `Validate Input`: https://docs.devexpress.com/Blazor/402066/components/data-editors/validate-input
- `Selection and Focus in Blazor Grid`: https://docs.devexpress.com/Blazor/404461/components/grid/selection-and-focus
- `DxGrid.ShowColumnChooser(...)`: https://docs.devexpress.com/Blazor/DevExpress.Blazor.DxGrid.ShowColumnChooser%28System.String%29


