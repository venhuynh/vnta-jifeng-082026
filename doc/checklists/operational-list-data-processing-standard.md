# Chuẩn Logic Xử Lý Dữ Liệu Màn Danh Sách Nghiệp Vụ

Tài liệu này mô tả chuẩn logic xử lý dữ liệu cho các màn `Operational List Page`
trong HRM, rút trực tiếp từ màn:

- [NhanVien.razor](../../src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/NhanVien/NhanVien.razor)
- [NhanVien.razor.cs](../../src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/NhanVien/NhanVien.razor.cs)

Mục tiêu là để các màn khác trong HRM bám cùng một flow dữ liệu, thay vì mỗi màn
tự nghĩ ra một kiểu `load/search/refresh/save` riêng.

## 1. Phạm vi áp dụng

Áp dụng khi màn có phần lớn các đặc điểm sau:

- `DxToolbar`
- `DxGrid` hoặc `DxTreeList`
- search theo từ khóa
- summary badge hoặc filter nhanh theo trạng thái
- action như `Làm mới`, `Đồng bộ`, `Xuất`, `Xóa`, `Chi tiết`
- popup `Create`, `Edit`, `Detail`

## 2. Nguyên tắc lõi

- UI chỉ giữ `view-state`.
- Server giữ business rule cuối cùng.
- Mọi trigger tải dữ liệu hội tụ về một entry point reload chung.
- Search, summary, paging và command nghiệp vụ phải dùng chung boundary
  `UI -> provider/typed client -> endpoint -> service -> database`.
- Không để grid tự thành nguồn sự thật cho filter/search nếu dữ liệu thật nằm ở
  server.

## 3. Flow chuẩn của màn

```text
firstRender
  -> ReloadAsync()
  -> provider summary query
  -> provider list query
  -> cập nhật SummaryBadges + Employees

search text changed / summary badge changed / refresh success / save success
  -> ReloadAsync()
  -> provider summary query
  -> provider list query
  -> cập nhật lại view-state
```

## 4. View-state chuẩn trong UI

Màn kiểu này nên có tối thiểu các nhóm state sau, đặt ở `.razor.cs`:

- Dữ liệu chính:
  - `Employees`
  - `SummaryBadges`
  - `SelectedDataItems`
- Bộ lọc và điều khiển:
  - `SearchText`
  - `ActiveSummaryBadgeKey`
  - `PageSize`
- Popup và form:
  - `DetailEmployee`
  - `CreateEmployeeModel`
  - `DepartmentOptions`
  - `PositionOptions`
  - `DetailPopupMode`
  - `IsDetailPopupVisible`
- Trạng thái bất đồng bộ:
  - `IsLoading`
  - `IsRefreshing`
  - `IsChangingPageSize`
  - `IsSavingEmployee`
  - `IsLoadingCreateLookups`
- Lỗi hiển thị:
  - `LoadErrorMessage`
  - `EditErrorMessage`
  - `CreateLookupErrorMessage`

Các state tương tác như `CanInteract`, `CanCreate`, `CanExport`,
`CanRefreshEmployees` nên là `derived state`, không hard-code trực tiếp tại
markup.

## 5. Tải dữ liệu lần đầu

Chuẩn tham chiếu từ `NhanVien`:

- tải dữ liệu tại `OnAfterRenderAsync(firstRender)` khi screen spec yêu cầu
  danh sách sẵn sàng ngay lúc mở
- không tải dữ liệu trực tiếp trong markup
- search hoặc filter reload theo screen spec nhưng phải dùng request snapshot và
  cancellation

Điều quan trọng không phải là bắt buộc dùng `OnAfterRenderAsync` cho mọi màn,
mà là:

- phải chốt rõ chiến lược load đầu tiên
- chỉ có một điểm mở màn chính
- không tạo nhiều điểm load ngầm chồng chéo

## 6. Entry point reload chung

Mọi trigger sau đều nên đi về một hàm chính như `ReloadAsync()`:

- mở màn lần đầu nếu screen tự tải
- đổi `SearchText`
- đổi `summary badge`
- refresh từ nguồn
- save create thành công
- save update thành công
- xóa thành công
- retry từ error state

Lý do:

- gom hành vi reload về một nơi
- tránh mỗi action reload kiểu riêng
- dễ khóa race condition

## 7. Guard chống reload chồng nhau

Chuẩn tham chiếu:

- `CancellationTokenSource` cho vòng đời component
- `SemaphoreSlim reloadGate`
- `reloadRequestedVersion`
- `reloadProcessedVersion`

Ý nghĩa:

- nếu người dùng gõ search liên tục, đổi badge, bấm refresh gần nhau thì màn
  không chạy nhiều đợt reload chồng chéo vô ích
- state cuối cùng luôn bám theo request mới nhất

Nếu màn sau không dùng đúng cùng cấu trúc này, vẫn phải có cơ chế tương đương.

## 8. Search chuẩn

Search chuẩn của màn danh sách nghiệp vụ nên đi theo các bước:

1. UI nhận text từ `DxSearchBox`
2. text được normalize:
   - `trim`
   - chuỗi rỗng thành `null`
3. nếu giá trị thực không đổi thì không reload
4. gọi `ReloadAsync()`
5. provider gửi filter xuống server
6. grid highlight lại từ khóa trên các cột text chính

Quy ước UI:

- dùng `DxSearchBox`
- `BindValueMode="BindValueMode.OnDelayedInput"`
- `InputDelay="300-500ms"`
- không dựa hoàn toàn vào `DxGrid.ShowSearchBox` cho luồng server-side

## 9. Summary badge chuẩn

Summary badge chỉ dùng khi màn có cohort nghiệp vụ rõ như:

- `Đang làm việc`
- `Thử việc`
- `Chính thức`
- `Nghỉ việc`
- `Tất cả`

Flow chuẩn:

1. summary query lấy số đếm
2. badge active map ra filter cụ thể
3. click badge đổi filter
4. gọi `ReloadAsync()`
5. grid hiển thị đúng tập dữ liệu tương ứng

Summary badge không được chỉ là trang trí. Nó phải thay đổi đúng dữ liệu thật.

## 10. Query summary và query list

Màn chuẩn kiểu `NhanVien` tách rõ:

- query summary:
  - trả `EmployeeSummaryDto`
- query list:
  - trả `EmployeeRecord`

Hai query này:

- có thể khác payload
- nhưng phải dùng cùng ngữ nghĩa filter hiện tại
- đặc biệt phải cùng hiểu `SearchText`

Nếu summary đếm theo toàn hệ thống còn grid lọc theo search hiện tại, người dùng
sẽ thấy UI mâu thuẫn.

## 11. Refresh từ nguồn ngoài CRUD

Action như `RefreshEmployeesFromAttendanceAsync()` là command nghiệp vụ riêng,
không phải CRUD thường.

Flow chuẩn:

1. chặn thao tác nếu màn đang loading hoặc refreshing
2. clear selection
3. gọi provider command
4. reload lại màn
5. hiển thị toast theo kết quả:
   - `warning` nếu không có nguồn
   - `info` nếu nguồn rỗng
   - `success` nếu có tạo/cập nhật

Các màn như `Phụ cấp`, `Khấu trừ`, `Chấm công` có action `refresh/sync` nên bám
đúng pattern này.

## 12. Page size và interaction gate

Nếu đổi `PageSize` gây thay đổi cảm nhận rõ, màn nên:

- có cờ riêng như `IsChangingPageSize`
- disable tương tác không cần thiết trong lúc chuyển
- đổi `LoadingText` đúng ngữ cảnh

Không nên để người dùng cảm giác bấm đổi page size mà màn “đơ” không phản hồi.

## 13. Selection và reload

Chuẩn tham chiếu:

- selection sống ở `SelectedDataItems`
- `GetSelectedEmployees()` lọc lại theo dữ liệu đang hiển thị
- trước `ReloadAsync()` hoặc `Refresh` cần `ClearSelectionAsync()`

Điều này tránh các lỗi:

- đang chọn dòng cũ nhưng grid đã đổi tập dữ liệu
- popup detail/edit bám nhầm bản ghi không còn visible

## 14. Popup create, edit, detail

### Create

Flow chuẩn:

1. mở popup
2. set mode `Create`
3. reset lỗi cũ
4. mở loading lookup
5. load danh mục phụ thuộc như phòng ban, chức vụ
6. cho phép submit

### Edit

Flow chuẩn:

1. yêu cầu đúng một dòng được chọn
2. map record hiện tại sang form model
3. load lookup như create
4. submit update
5. reload list

### Detail

Flow chuẩn:

- không reload riêng nếu record đã có sẵn đủ dữ liệu
- mở popup nhẹ, không mutate form model

## 15. Validation và lỗi

Chuẩn từ `NhanVien`:

- validation field-level nằm trong popup
- lỗi nghiệp vụ server-side quay về `EditErrorMessage`
- popup không đóng nếu save lỗi
- load failure của toàn màn hiển thị `error-state` riêng với nút `Thử lại`

Không dùng toast làm nơi hiển thị validation chính.

## 16. Success feedback

Sau các action quan trọng, phải có feedback rõ:

- create success
- update success
- delete success
- export started
- refresh/sync success

Toast nên đi qua `IHrmToastService`.

## 17. Concurrency gate

Chuẩn hiện tại của `NhanVien`:

- optimistic concurrency theo `OriginalUpdatedAtUtc`
- server từ chối lưu nếu bản ghi đã bị đổi bởi phiên khác

Chuẩn đích cứng hơn có thể là `RowVersion`, nhưng trong lúc chưa nâng lên, các
màn tương tự vẫn phải có một gate tương đương, không để update “last write wins”
âm thầm.

## 18. Những gì màn khác phải copy đúng tinh thần

- một reload entry point chung
- debounce search
- summary badge lọc dữ liệu thật
- clear selection trước reload lớn
- loading text theo ngữ cảnh
- error state có retry
- popup save fail không đóng
- toast phản hồi rõ ràng
- logic nặng nằm ở provider/server, không nằm trong Razor markup

## 19. Anti-pattern cần tránh

- dùng `ShowSearchBox` rồi lại tự xử lý search server-side theo đường khác
- để summary badge chỉ đổi màu mà không đổi filter
- để search text đổi là bắn nhiều reload song song không kiểm soát
- save xong cập nhật chắp vá client state nhưng không reload nguồn thật
- dùng một cờ `IsBusy` cho mọi tình huống, khiến UI không phân biệt được
  `loading`, `refresh`, `save`, `page size`
- đóng popup ngay cả khi server trả lỗi nghiệp vụ

## 20. Checklist đọc kèm

- `doc/checklists/screen-implementation-principles.md`
- `doc/checklists/ui-screen-checklist.md`
- `doc/checklists/operational-list-screen-checklist.md`
- `doc/checklists/ui-state-checklist.md`
- `doc/checklists/data-screen-performance-refactor-checklist.md` khi màn có dữ liệu lớn, ma trận hoặc refactor read path.
- `doc/checklists/done-checklist.md`
- `doc/screens/nhan-su/nhan-vien-trien-khai-mau.md`


