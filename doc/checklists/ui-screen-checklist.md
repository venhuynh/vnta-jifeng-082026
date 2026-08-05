# Checklist Tạo Màn Hình UI

Áp dụng khi tạo mới một màn hình UI cho `src/Vnta.HRM2026`, nhất là các màn
nghiệp vụ DevExpress Blazor kiểu danh sách, quản trị hoặc vận hành.

Nếu screen spec có yêu cầu đặc thù khác checklist này, ưu tiên screen spec
nhưng vẫn phải giữ các guardrail chung của repo.

## 1. Trước khi code

- [ ] Đã đọc `doc/checklists/screen-implementation-principles.md`.
- [ ] Nếu đây là màn danh sách nghiệp vụ, đã đọc thêm `doc/checklists/operational-list-screen-checklist.md`.
- [ ] Nếu đây là màn danh sách nghiệp vụ kiểu `NhanVien`, đã đọc thêm `doc/checklists/operational-list-data-processing-standard.md`.
- [ ] Nếu màn có popup mở từ UI chính, đã đọc thêm `doc/checklists/ui-popup-checklist.md`.
- [ ] Nếu màn có popup xác nhận lại trước khi chạy command, đã đọc thêm `doc/checklists/confirmation-popup-checklist.md`.
- [ ] Nếu là màn mới hoặc refactor lớn, đã mở và điền `doc/templates/screen-implementation-template.md`.
- [ ] Đã đọc screen spec hoặc blueprint liên quan trong `doc/project/`.
- [ ] Đã chốt `bounded context`, `context key` bằng tiếng Việt không dấu và đối chiếu `doc/project/feature-folder-standard.md`.
- [ ] Đã xác định rõ luồng `UI -> provider/typed client -> endpoint -> service -> database` của màn.
- [ ] Đã chốt rõ business rule nào nằm ở server, UI chỉ giữ view-state, validation nhẹ và preview.
- [ ] Đã chốt route, permission, DTO hoặc filter hoặc request và service dùng cho màn.
- [ ] Đã xác nhận màn mới đi theo `InteractiveServer`, không tự thêm WASM render mode.
- [ ] Đã xác định đây là `Master Data List Page`, `Operational List Page` hay biến thể khác.
- [ ] Đã chốt chiến lược tải dữ liệu lần đầu của màn: tự tải ở `firstRender` hay chờ hành động người dùng; không để phát sinh nhiều điểm load ngầm khó kiểm soát.
- [ ] Nếu màn có action nghiệp vụ như `refresh`, `sync`, `lock/unlock`, `approve`, `retry`, đã xác định chúng đi qua command riêng thay vì nhét chung vào CRUD.
- [ ] Nếu màn có nguy cơ nhiều người cùng sửa, đã xác định cơ chế concurrency hoặc lock state.
- [ ] Đã đối chiếu `doc/rules/devexpress-icon-rules.md`; mọi icon UI của màn phải dùng DevExpress Icon Library qua `IconUrl` hoặc `VntaDevExpressIcons`, không dùng Bootstrap Icons, CDN `bootstrap-icons`, class `bi` hoặc `bi-*`.

## 2. Cấu trúc màn hình

- [ ] Màn hình nằm trong đúng feature folder của source hiện hành `src/Vnta.HRM2026/...`.
- [ ] Folder, file và class liên quan đến feature dùng cùng một `context key` bằng tiếng Việt không dấu, không lệch ngữ cảnh giữa UI và backend.
- [ ] Page hoặc component production được tách thành `*.razor`, `*.razor.cs`, `*.razor.css`.
- [ ] Màn UI tiêu chuẩn có file chính và file popup/form tách riêng: `Screen.razor`, `Screen.razor.cs`, `Screen.razor.css`, `ScreenEditForm.razor`; popup chi tiết hoặc popup nghiệp vụ độc lập dùng `*Popup.razor`.
- [ ] Không để business logic, query dữ liệu hoặc orchestration dài trong `@code` của `.razor`.
- [ ] File `*.razor.cs` được nhóm rõ theo `Constants`, `Dependencies`, `State`, `Derived State`, `UI Entry Points`, `Data Loading`, `Screen Actions`, `Helpers`, `Disposal` hoặc nhóm tương đương; không để luồng xử lý bị đan xen khó đọc.
- [ ] Không inject `ApplicationDbContext` hoặc truy vấn EF Core trực tiếp trong screen UI.
- [ ] Edit model hoặc view model của UI không dùng chung trực tiếp với EF entity hoặc row persistence.
- [ ] Child component chỉ được tách ra khi có trách nhiệm rõ ràng như toolbar, grid, popup hoặc detail.

## 3. Layout và composition

- [ ] Bố cục chính rõ ràng theo khung `toolbar + primary data surface`.
- [ ] File chính của màn tiêu chuẩn ưu tiên bám skeleton đã ổn định ở `NhanVien`: `content-root` -> `card toolbar` -> `screen-root` hoặc `*-root` -> `card primary surface` -> `HrmLoadingPanel` -> `grid content` -> `screen header` -> `DxGrid` hoặc `DxTreeList`.
- [ ] Toolbar dùng `DxToolbar Title="..." ItemRenderStyleMode="ToolbarRenderStyleMode.Plain"` và action bên phải theo thứ tự `Mới`, `Điều chỉnh`, `Xóa`, `Làm mới`, action nghiệp vụ riêng nếu có, `Chi tiết` nếu cần, `Xuất dữ liệu`, `Chọn cột`.
- [ ] Error state nằm trong `card error-state`; empty state nằm trong `EmptyDataAreaTemplate`; edit form nằm trong `EditFormTemplate` và gọi component `ScreenEditForm`.
- [ ] Popup edit form của `DxGrid`/`DxTreeList` tự render footer `Lưu` và `Hủy` bằng `DxButton`, dùng `IconUrl="@VntaDevExpressIcons.Save"` và `IconUrl="@VntaDevExpressIcons.Cancel"`; không để nút mặc định tiếng Anh `Save`/`Cancel`.
- [ ] Popup độc lập nếu có được render ngoài `content-root`, giống `AttendanceDeviceInfoPopup` của màn `MayChamCong`.
- [ ] Mọi popup của màn, gồm popup edit, popup rules, popup chọn phạm vi và popup xác nhận, đều phải tuân thủ `doc/checklists/ui-popup-checklist.md`; popup xác nhận command còn phải tuân thủ thêm `doc/checklists/confirmation-popup-checklist.md`.
- [ ] Với màn `Operational List Page`, nếu có nhóm trạng thái nghiệp vụ rõ như `Đang làm việc`, `Thử việc`, `Chính thức`, `Nghỉ việc`, ưu tiên dùng summary badge strip ngay trong header của data surface để lọc nhanh.
- [ ] Với màn `Master Data List Page`, không tự thêm summary badge strip hoặc status band nếu spec không yêu cầu và nếu dữ liệu không có cohort vận hành đủ rõ.
- [ ] Với màn cần inspect record liên tục, detail dùng `DxDrawer` hoặc `DxPopup` thay vì điều hướng rời context.
- [ ] Search, summary badge, refresh, export và column chooser phải nằm sát primary surface; search không bắt buộc nằm trong `DxToolbar`, có thể đặt ở `screen header` cùng hàng với summary badge như `NhanVien`.

## 4. CSS và styling

- [ ] Ưu tiên scoped CSS trong `*.razor.css`, không vá style inline.
- [ ] CSS page tiêu chuẩn giữ skeleton dùng chung của repo: `.content-root`, `.toolbar`, `.*-root`, `.card`, `.empty-state`, `.error-state`, `.state-title`, `.state-message`, `::deep .*-loading-panel`, `::deep .*-grid`, `::deep .*-popup`; nếu màn có `screen header` thì định nghĩa thêm block riêng cho `summary strip` và `search`.
- [ ] Tên class riêng của grid/loading/popup dùng tiền tố theo màn, không copy nguyên `attendance-devices-*` sang màn khác.
- [ ] Không tạo one-off global utility class hoặc một hệ CSS song song với pattern đang có.
- [ ] Ưu tiên reuse theme token, spacing, visual style và shell hiện có của repo.
- [ ] Nếu có grid hoặc tree, đã dùng class riêng theo screen thay vì tái dùng class của màn khác.

## 5. Grid hoặc tree hoặc form

- [ ] Nếu dùng `DxGrid`, đã chọn đúng một mode: `paging`, `virtual scrolling` hoặc `bounded all rows`.
- [ ] Nếu dùng `DxGrid`, bắt buộc có cột `STT` dùng `context.VisibleIndex + 1`, đặt ngay sau selection/command hoặc là cột đầu tiên nếu grid read-only.
- [ ] Nếu dùng `bounded all rows`, tập dữ liệu có giới hạn tự nhiên nhỏ và không bật pager/virtual scrolling.
- [ ] Nếu grid có selection, có `KeyFieldName` ổn định và selection state sống ở `.razor.cs`.
- [ ] Nếu màn dùng ô tìm kiếm riêng thay cho `ShowSearchBox`, ô tìm kiếm dùng `DxSearchBox` với `BindValueMode.OnDelayedInput`, `InputDelay` phù hợp (`300-500ms`) và callback rõ ràng về một entry point reload.
- [ ] Nếu màn có search text trên dữ liệu hiển thị, các cột text chính nên hỗ trợ highlight từ khóa theo cách an toàn HTML, tương tự `HighlightSearchText(...)` ở `NhanVien`.
- [ ] Nếu có manual refresh, refresh đó clear selection trước khi reload.
- [ ] Nếu thay đổi `PageSize` làm đổi trải nghiệm thấy rõ, màn cần có loading state hoặc interaction gate riêng cho thao tác này.
- [ ] Nếu màn có summary badge strip, chọn badge phải dẫn về đúng filter server-side hoặc provider-side, không chỉ lọc hình thức ở giao diện.
- [ ] Grid hoặc tree có host height rõ ràng, không phó mặc sizing hoàn toàn cho wrapper ngẫu nhiên.
- [ ] Form thêm hoặc sửa dùng editor DevExpress phù hợp và validation hiển thị ngay trong form.
- [ ] Form tuân thủ `doc/rules/devexpress-input-validation-rules.md`: edit context, DataAnnotations, two-way binding, message không bị lặp.

## 6. State và feedback

- [ ] Màn có đủ `Loading`, `Empty`, `Error`, `Success`.
- [ ] Loading dùng `DxLoadingPanel` khi phù hợp và không làm trắng toàn bộ shell nếu không cần.
- [ ] Loading text phản ánh đúng hành động đang diễn ra nếu màn có nhiều loại tải như `tải lần đầu`, `làm mới`, `đổi page size`, `đồng bộ`, `lưu`.
- [ ] Toast bắt buộc đi qua `IHrmToastService` theo `doc/rules/shared-toast-rules.md`.
- [ ] Business page hoặc popup nghiệp vụ không inject `IToastNotificationService`, không render `DxToastProvider` và không tự giữ provider toast cục bộ.
- [ ] Dialog dùng shared service hoặc shared provider của host, không tạo provider cục bộ trong business page.
- [ ] Sau create hoặc update hoặc delete hoặc export hoặc action quan trọng, người dùng nhận được feedback rõ ràng.
- [ ] Error state phải có thông điệp ngắn gọn và nút thử lại hoặc hành động hồi phục tương ứng, không chỉ hiện toast rồi để màn trắng.
- [ ] State bind cho grid, tree, detail drawer hoặc popup không bị mutate trực tiếp khi có async update.
- [ ] Mọi reload chính của màn đi qua một entry point chung như `ReloadAsync()`; nếu có nhiều nguồn trigger bất đồng bộ, phải có gate tránh reload chồng nhau và tránh ghi đè state cũ.
- [ ] Các cờ thao tác như `CanInteract`, `CanCreate`, `CanExport`, `CanRefresh...` được suy ra từ state hiện tại thay vì hard-code rải rác trên từng nút.
- [ ] Timer, SignalR hoặc async callback muộn có guard dispose hoặc cancel và đi qua `InvokeAsync(...)`.
- [ ] Nếu màn có realtime hoặc auto-refresh hoặc callback nền, đã có smoke test nhiều tab trước khi chốt.

## 7. Wiring và review cuối

- [ ] Menu, route, permission và DI của màn mới khớp nhau.
- [ ] Caption UI mới là tiếng Việt có dấu, ngắn gọn và đúng ngữ cảnh HRM.
- [ ] Đã cập nhật tài liệu liên quan nếu màn này tạo ra pattern UI lặp lại mới như `summary badge + search header`, `deferred reload`, `search highlight`, `loading gate` hoặc `detail popup`.
- [ ] Đã cập nhật đúng file ngày trong `doc/implementation-log/yyyyMMdd.md`.

## Tài liệu nên đọc kèm

- `doc/checklists/screen-implementation-principles.md`
- `doc/checklists/operational-list-screen-checklist.md`
- `doc/project/hrm-list-screen-blueprint.md`
- `doc/rules/blazor-devexpress-rules.md`
- `doc/rules/devexpress-icon-rules.md`
- `doc/rules/grid-rules.md`
- `doc/checklists/ui-state-checklist.md`
- `doc/checklists/ui-popup-checklist.md`
- `doc/checklists/confirmation-popup-checklist.md`
- `doc/checklists/done-checklist.md`


