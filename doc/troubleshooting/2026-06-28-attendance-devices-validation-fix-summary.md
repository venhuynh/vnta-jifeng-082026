# Bug fix summary: popup `Máy chấm công` không mở do validation

## Ngày

- `2026-06-28`

## Màn hình hiện hành

- Menu: `Quản trị > Máy chấm công`
- Component hiện hành: `MayChamCong`

## Ghi chú lịch sử

- Tại thời điểm fix, màn này còn dùng tên `AttendanceDevices`.
- Tên file hiện hành tương đương:
  - `AttendanceDevices.razor` -> `MayChamCong.razor`
  - `AttendanceDeviceEditForm.razor` -> `MayChamCongEditForm.razor`

## Tóm tắt lỗi

Các `DxTextBox` read-only trong form edit dùng one-way binding `Text=` nhưng không tắt validation, làm popup crash khi render.

## Hướng sửa đã chốt

- Thêm `ValidationEnabled="false"` cho các `DxTextBox` read-only chỉ dùng để hiển thị.

## File hiện hành tương ứng

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCongEditForm.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCong.razor.css`

## Bài học rút ra

- Read-only text field của DevExpress không nên để validation chạy nếu chỉ bind bằng `Text=`.
- Khi popup không mở, ưu tiên đọc stack trace render component trước khi nghi ngờ logic thao tác hoặc permission.
