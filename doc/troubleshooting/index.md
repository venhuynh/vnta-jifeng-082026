# Troubleshooting

## Tổng quan

Thư mục này lưu các sự cố thực tế, root cause và hướng xử lý đã áp dụng trong repo.

## Ghi chú về baseline hiện hành

- Source HRM hiện hành của repo là `src/Vnta.HRM2026`.
- Từ baseline menu/source ngày `2026-07-01`, nhiều màn đã đổi tên file hoặc đổi vị trí thư mục.
- Vì vậy một số issue lịch sử có thể còn nhắc tới tên/path cũ như:
  - `AttendanceDevices`
  - `AttendanceLogs`
  - `AdmsMonitor`
  - `Components/Attendance/...`
  - `Components/Implementation/...`

Khi cần tìm source hiện hành, luôn đối chiếu thêm:

- `doc/project/source-map.md`
- `doc/project/menu-sync-20260701.md`
- `doc/rules/menu-structure-rules.md`

## Danh mục hiện có

### Render và Loading

- [Quan Tri menu loading overlay fix](./2026-07-01-quan-tri-menu-loading-overlay.md)
  Playbook sửa lỗi các màn hình trong menu `Quản trị` bị treo global Loading khi mở nhiều tab hoặc click nhiều node menu liên tiếp.

### DevExpress Blazor

- [Playbook fix lỗi NavMenu + DxTreeView](./navmenu-dxtreeview-playbook.md)
  Hướng dẫn tác chiến cho AI agent khi branch bị lệch NavMenu, icon, current module hoặc render tree của `DxTreeView`.
- [Lỗi Validation của DxTextBox khi dùng one-way binding](./devexpress-textbox-validation-error.md)
  Ghi nhận sự cố popup của màn `Máy chấm công`, tên lịch sử trong log là `AttendanceDevices`.
- [DevExpress TextBox Quick Reference](./devexpress-textbox-quick-reference.md)
  Tóm tắt pattern binding và validation nên dùng trong repo.

## Quy tắc đọc đúng ngữ cảnh

- Nếu tài liệu troubleshooting mô tả stack trace hoặc log cũ, giữ nguyên tên lịch sử trong phần log là chấp nhận được.
- Nếu cần đi tới file hiện hành để sửa code, phải dùng source path hiện tại trong `source-map.md`.
- Khi viết troubleshooting mới, ưu tiên dùng tên màn hình và path hiện hành, chỉ nhắc tên cũ khi thật sự cần để đối chiếu log lịch sử.




