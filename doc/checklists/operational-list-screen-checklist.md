# Checklist Chuẩn Màn Hình Danh Sách Nghiệp Vụ

Áp dụng cho các màn hình kiểu danh sách nghiệp vụ trong `src/Vnta.HRM2026`,
đặc biệt là các màn có:

- `DxToolbar`
- `DxGrid` hoặc `DxTreeList`
- search hoặc filter hoặc summary badge
- action như `refresh`, `sync`, `export`, `column chooser`

Checklist này bổ sung cho:

- `doc/checklists/screen-implementation-principles.md`
- `doc/checklists/ui-screen-checklist.md`
- `doc/checklists/ui-state-checklist.md`

Nó không thay thế các checklist gốc. Mục tiêu là chuẩn hóa trải nghiệm của các
màn danh sách vận hành để những màn sau không bị lệch bố cục, lệch hành vi
search hoặc lặp lại anti-pattern đã xử lý ở nhánh này.

Màn tham chiếu chuẩn hiện tại của checklist này là:

- [NhanVien.razor](../../src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/NhanVien/NhanVien.razor)
- [NhanVien.razor.cs](../../src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/NhanVien/NhanVien.razor.cs)
- `doc/checklists/operational-list-data-processing-standard.md`

## 1. Xác định đúng loại màn

- [ ] Đã xác nhận đây là `Operational List Page` hoặc biến thể gần tương đương, không phải form đơn, dashboard hay màn wizard.
- [ ] Đã đối chiếu `doc/project/hrm-list-screen-blueprint.md` trước khi chốt layout.
- [ ] Đã xác định rõ màn này có cần `summary badge` hay không; nếu chỉ là CRUD danh mục đơn giản thì không tự thêm dải summary.
- [ ] Đã xác định search của màn là search nghiệp vụ server-side, không phó mặc toàn bộ cho filter cục bộ của grid.
- [ ] Đã xác định màn này có cần flow dữ liệu kiểu `NhanVien` gồm `firstRender`, `ReloadAsync()`, `summary query`, `list query`, `refresh command`, `popup create/edit/detail`.

## 2. Phân tầng điều khiển trong UI

- [ ] Toolbar chỉ giữ action chính của màn: `Mới`, `Điều chỉnh`, `Xóa`, `Làm mới`, action nghiệp vụ riêng, `Xuất dữ liệu`, `Chọn cột`.
- [ ] Search, summary badge và filter trạng thái được gom gần `primary data surface`, không nhét lẫn với toolbar action nếu chúng mang vai trò data shaping.
- [ ] Nếu có cả search và summary badge, chúng nằm chung một cụm header phía trên grid hoặc tree để người dùng hiểu đây là bộ điều khiển dữ liệu.
- [ ] Không tạo nhiều dải control cạnh tranh nhau về vai trò trong cùng một viewport.
- [ ] `Chi tiết` nếu có là action riêng ở toolbar hoặc vùng thao tác, không bị nhét lẫn vào summary hoặc filter header.

## 3. Layout chuẩn cho màn danh sách

- [ ] Bố cục đi theo khung: `content-root` -> `card toolbar` -> `card data surface`.
- [ ] Khối dữ liệu chính có header rõ ràng nếu cần chứa search hoặc summary badge.
- [ ] Header của khối dữ liệu hòa vào mặt bảng bằng nền và viền nhẹ, không tạo cảm giác là một card thứ hai chồng lên grid.
- [ ] Search box có chiều rộng ổn định, không làm nhảy layout khi text dài hoặc viewport hẹp.
- [ ] Summary badge trên desktop ưu tiên nằm một hàng có scroll ngang nhẹ; trên mobile có thể wrap.
- [ ] Error state và empty state vẫn nằm trong cùng context của data surface, giống cách `NhanVien` đặt `error-state` và `EmptyDataAreaTemplate`.

## 4. Summary badge và bộ lọc nhanh

- [ ] Summary badge chỉ xuất hiện khi có ý nghĩa nghiệp vụ thật sự như trạng thái, mức ưu tiên, nhóm xử lý hoặc nguồn dữ liệu.
- [ ] Tên badge là tiếng Việt có dấu, ngắn, quét mắt được.
- [ ] Badge active có trạng thái đủ rõ nhưng không áp đảo toàn bộ màn hình.
- [ ] Màu badge đi theo semantic token của hệ thống như `info`, `warning`, `success`, `danger`; không hardcode màu tùy hứng nếu repo đã có token phù hợp.
- [ ] Số lượng trên badge dùng `tabular-nums` hoặc style ổn định để tránh rung thị giác khi thay đổi.
- [ ] Click badge thay đổi filter server-side và reload đúng tập dữ liệu.
- [ ] Khi badge đang active, disabled state không làm badge active trông như bị hỏng.

## 5. Search và hành vi tìm kiếm

- [ ] Search của màn có `InputDelay` hoặc cơ chế debounce phù hợp, không bắn reload liên tục theo từng ký tự nếu dữ liệu đi qua server.
- [ ] Search text được normalize trước khi reload: trim khoảng trắng, chuỗi rỗng quy về `null`.
- [ ] Nếu giá trị search thực tế không đổi, không reload lại màn.
- [ ] Search server-side và highlight từ khóa trên grid phải dùng cùng một nguồn text để người dùng không thấy kết quả lệch nhau.
- [ ] Search thay đổi phải đi về cùng một entry point reload với summary badge và refresh, không tạo pipeline riêng lệch trạng thái.
- [ ] Nếu cột grid dùng `CellDisplayTemplate`, đã xử lý highlight riêng cho cột đó thay vì giả định grid tự highlight.
- [ ] Từ khóa khớp trên dữ liệu được tô sáng đủ rõ, thường bằng `<mark>` hoặc template highlight tương đương.
- [ ] Màu highlight đủ tương phản nhưng không phá readability của trạng thái row đang focus hoặc selected.

## 6. Grid, tree và vùng dữ liệu chính

- [ ] Grid có `KeyFieldName` ổn định.
- [ ] Có cột `STT` dùng `context.VisibleIndex + 1` nếu đây là màn danh sách tiêu chuẩn.
- [ ] `PageSize`, `PageSizeChanged` và loading state của paging được xử lý rõ ràng nếu người dùng đổi page size.
- [ ] `EmptyDataAreaTemplate` có nội dung đúng theo từng tình huống: chưa có dữ liệu, không có kết quả search, hoặc không có dữ liệu trong trạng thái đã chọn.
- [ ] `error-state` không đẩy người dùng ra khỏi context; vẫn có nút thử lại hoặc recovery path rõ ràng.
- [ ] `HrmLoadingPanel` bao phủ đúng vùng dữ liệu cần chặn, bao gồm header filter nếu header đó là một phần của data surface.
- [ ] Refresh hoặc sync phải clear selection trước khi reload.
- [ ] Selection helper phải lọc lại theo `VisibleEmployees` hoặc tập dữ liệu đang hiển thị, tránh giữ selected row “ma” sau reload.

## 7. Đồng bộ state bất đồng bộ

- [ ] Reload dữ liệu có guard để tránh chạy chồng nhiều request cùng lúc khi search, refresh, đổi badge hoặc đổi page size diễn ra liên tiếp.
- [ ] Nếu dùng `CancellationTokenSource`, đã dispose đúng chỗ.
- [ ] Callback async muộn không mutate state sau khi component đã dispose.
- [ ] State như `IsLoading`, `IsRefreshing`, `IsChangingPageSize`, `HasLoadError` đã được tách rõ, không dùng một cờ chung cho mọi tình huống.
- [ ] Những action cần disable UI đã bind vào derived state rõ ràng như `CanInteract`, `CanExport`, `CanRefresh`.
- [ ] Nếu màn có popup create/edit cần lookup phụ, đã có state riêng kiểu `IsLoadingCreateLookups`, không dùng chung với loading của list.

## 8. Action nghiệp vụ và boundary dữ liệu

- [ ] Search hoặc summary hoặc filter đi qua `provider/typed client -> endpoint -> service`, không nhồi business query vào UI.
- [ ] Action như `refresh from source`, `sync`, `export selected`, `delete selected` có feedback thành công hoặc thất bại rõ ràng.
- [ ] Nếu action update có nguy cơ ghi đè do nhiều người cùng sửa, đã xác định concurrency gate hoặc cơ chế tương đương.
- [ ] UI chỉ phản ánh kết quả cuối cùng từ server; không tự coi client state là nguồn sự thật cho nghiệp vụ.
- [ ] Save create/update thành công phải quay về reload nguồn thật hoặc cơ chế tương đương, không vá cục bộ list nếu chưa chứng minh chắc chắn dữ liệu phụ thuộc không đổi.

## 9. Chất lượng UX cần chốt trước khi nhân rộng

- [ ] Toolbar action, summary badge, search box và grid không cạnh tranh màu nhấn chính với nhau.
- [ ] Màn hình vẫn dễ scan khi số cột nhiều hoặc dữ liệu dài.
- [ ] Text dài trong cột như phòng ban hoặc chức vụ có ellipsis hoặc cách xử lý tràn phù hợp.
- [ ] Search box, badge, empty state, error state và loading state đều dùng tiếng Việt có dấu, đúng ngữ cảnh HRM.
- [ ] Keyboard focus nhìn thấy được ở badge, search và button quan trọng.
- [ ] Màn hình không tạo thêm một hàng control chỉ để “cho đủ”, mọi control đều có vai trò rõ.

## 10. Kiểm chứng trước khi xem là chuẩn

- [ ] Đã build thành công màn hoặc project liên quan sau khi hoàn tất.
- [ ] Đã smoke test ít nhất các luồng: mở màn, search, đổi badge, refresh, đổi page size, chọn dòng, mở popup, quay lại list.
- [ ] Đã kiểm tra trường hợp search không có kết quả và trường hợp danh sách rỗng hoàn toàn.
- [ ] Đã kiểm tra trạng thái loading khi search nhanh hoặc thao tác liên tiếp, bảo đảm không có race condition rõ ràng.
- [ ] Nếu checklist này sinh ra từ một màn đã triển khai, đã đối chiếu lại màn đó để chắc tài liệu và thực tế không lệch nhau.

## Tài liệu nên đọc kèm

- `doc/checklists/screen-implementation-principles.md`
- `doc/checklists/ui-screen-checklist.md`
- `doc/checklists/ui-state-checklist.md`
- `doc/checklists/done-checklist.md`
- `doc/project/hrm-list-screen-blueprint.md`
- `doc/checklists/operational-list-data-processing-standard.md`


