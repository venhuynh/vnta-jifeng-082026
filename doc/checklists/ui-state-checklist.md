# Checklist UI State

Áp dụng cho mọi màn hình DevExpress Blazor có tải dữ liệu, thao tác lưu hoặc
gọi action nghiệp vụ.

Với màn danh sách nghiệp vụ trong HRM, chuẩn state này được neo theo logic của:

- [NhanVien.razor](../../src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/NhanVien/NhanVien.razor)
- [NhanVien.razor.cs](../../src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/NhanSu/NhanVien/NhanVien.razor.cs)

## State bắt buộc

Mỗi data screen phải nghĩ rõ và thể hiện được 4 trạng thái:

- `Loading`
- `Empty`
- `Error`
- `Success`

## 1. Loading

- [ ] Initial load có loading state rõ ràng.
- [ ] Reload grid hoặc tree có loading indicator.
- [ ] Nếu màn có nhiều loại tải như `initial load`, `refresh`, `page size`, `save`, mỗi loại tải có cờ state riêng hoặc ít nhất có phân biệt đủ rõ trong `LoadingText`.
- [ ] Save action disable nút submit khi đang chạy.
- [ ] Long-running action hoặc export có feedback đang xử lý.
- [ ] Dùng `HrmLoadingPanel` khi phù hợp thay vì làm trắng cả page shell.
- [ ] Text loading dùng chung theo chuẩn HRM, không hard-code `Loading...`.

## 2. Empty

- [ ] Empty state nói rõ không có dữ liệu.
- [ ] Empty state phản ánh đúng filter hiện tại.
- [ ] Có reset filter khi workflow cần.
- [ ] Nếu màn có search và summary badge, empty state phân biệt được `không có kết quả tìm kiếm`, `không có dữ liệu cho badge hiện tại` và `toàn bộ danh sách đang rỗng`.
- [ ] Empty state không bị hiểu nhầm là lỗi hệ thống.

## 3. Error

- [ ] Data load failure có retry path.
- [ ] Save failure không làm mất input người dùng.
- [ ] Lỗi field hiển thị gần field hoặc trong form.
- [ ] Lỗi chung hiển thị an toàn, không lộ stack trace.
- [ ] Error message đúng ngữ cảnh nghiệp vụ.
- [ ] Nếu lỗi ở màn danh sách chính, màn vẫn giữ `error-state` trong context của data surface và có nút `Thử lại`, thay vì chỉ bắn toast.

## 4. Success

- [ ] Save success đóng popup hoặc cập nhật state có chủ đích.
- [ ] Grid hoặc tree refresh sau create hoặc update hoặc delete.
- [ ] Export hoặc action thành công có feedback rõ.
- [ ] Approval hoặc retry hoặc cancel có toast hoặc tín hiệu thành công.
- [ ] Toast success hoặc info hoặc warning hoặc error đi qua `IHrmToastService`, không gọi raw DevExpress toast API trong screen.
- [ ] Sau command kiểu `refresh/sync`, success state phản ánh đúng trường hợp `không có nguồn`, `nguồn rỗng`, `đồng bộ thành công`.

## 5. Theo loại màn hình

### Grid list

- [ ] Grid-level loading xuất hiện khi reload.
- [ ] Empty state rõ nghĩa.
- [ ] Toolbar action vẫn nhất quán theo permission và state.
- [ ] Selection được clear trước các reload lớn như refresh hoặc sync nếu workflow giống `NhanVien`.

### Edit popup

- [ ] Save button disable khi submit.
- [ ] Validation ở trong popup.
- [ ] Save fail không đóng popup.
- [ ] Nếu popup cần lookup phụ thuộc, popup có loading state riêng cho lookup và không cho submit sớm khi lookup chưa sẵn sàng.

### Detail drawer

- [ ] Drawer có loading riêng nếu fetch detail bất đồng bộ.
- [ ] Detail lỗi có retry hoặc thông báo an toàn.

### Dashboard

- [ ] KPI hoặc chart có loading riêng khi phù hợp.
- [ ] Empty state không làm hỏng bố cục tổng.

## 6. Render stability

- [ ] Collection hoặc row đang bind cho UI không bị mutate trực tiếp nếu màn có update thường xuyên.
- [ ] Các trigger `search`, `summary badge`, `refresh`, `retry` nên đi về cùng một hàm reload để state nhất quán.
- [ ] Timer, SignalR hoặc async callback muộn chỉ cập nhật UI qua `InvokeAsync(...)`.
- [ ] Callback nền có guard cancel hoặc dispose trước khi chạm vào state màn hình.
- [ ] Save pipeline không chồng nhiều cơ chế refresh không cần thiết.
- [ ] Nếu search có debounce, màn có guard tránh reload chồng nhau hoặc trả về ngược thứ tự.
- [ ] Nếu có `Grid.Reload()`, đã xác nhận đây là fallback có chủ đích chứ không phải thói quen.
- [ ] Màn có realtime, timer hoặc auto-refresh đã được smoke test với nhiều tab.

## 7. Hoàn tất

- [ ] Screen đã được review đủ 4 state.
- [ ] Screen không im lặng khi action thành công hoặc thất bại.
- [ ] Rule state này đã được đối chiếu với checklist hoàn tất chung.
