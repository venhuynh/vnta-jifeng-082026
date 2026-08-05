# Attendance Device Serial Guard

## Mục tiêu

Khóa chặt dữ liệu `devices.SerialNumber` để gateway và màn `Máy chấm công` không còn gặp lỗi do trùng serial.

## Dữ liệu đã rà

Ngày `2026-07-07` đã xác nhận có 1 nhóm serial trùng trong DB:

- `CQUG221860047`
- `5aa07dc1-9267-4e45-86cd-e2042dd8b3d7`
- `10000000-0000-0000-0000-000000000001`

Hai dòng này là nguyên nhân trực tiếp làm `BioDataSyncService.ProcessAsync(...)` nổ lỗi `Sequence contains more than one element` khi tra cứu thiết bị theo serial.

## Quy tắc bắt buộc

- Chỉ lưu serial đã chuẩn hóa theo `NormalizeSerial(...)`.
- Giá trị lưu cuối cùng chỉ gồm chữ cái tiếng Anh in hoa và chữ số.
- Một serial không rỗng chỉ được phép tồn tại đúng một lần trong bảng `devices`.
- `attendance_logs.DeviceId` phải được remap sang dòng giữ lại trước khi xóa record trùng serial.

## Guard triển khai

### 1. Guard khi khởi động

Cả `HRM` và `gateway` đều phải chạy guard trước khi xử lý nghiệp vụ:

1. Chuẩn hóa lại toàn bộ `devices.SerialNumber`.
2. Gom nhóm theo serial đã chuẩn hóa.
3. Chọn một dòng giữ lại.
4. Remap `attendance_logs.DeviceId` từ dòng trùng sang dòng giữ lại.
5. Xóa các dòng trùng.
6. Tạo unique partial index trên `devices("SerialNumber")` với điều kiện serial không rỗng.

### 2. Guard ở mức schema

Tên index chuẩn:

- `ux_devices_serial_number_not_empty`

Ý nghĩa:

- không cho thêm mới hoặc cập nhật để tạo ra serial trùng;
- không chặn các record hệ thống không dùng serial vì index chỉ áp dụng cho serial không rỗng.

### 3. Guard ở màn `MayChamCong`

- UI chỉ cho lưu serial dạng chữ và số.
- `DatabaseAttendanceDeviceService.ValidateAsync(...)` vẫn chặn trùng serial ở tầng nghiệp vụ.
- `DatabaseAttendanceDeviceService.SaveAsync(...)` phải catch unique-violation của PostgreSQL để trả ra thông báo nghiệp vụ thay vì lộ lỗi DB.

## Lưu ý vận hành

- Nếu build `gateway` đang bị file lock bởi tiến trình chạy nền, có thể chưa xác nhận được bước copy output cuối, nhưng guard/schema vẫn phải được giữ trong source.
- Sau khi deploy lại, cần kiểm tra log khởi động để xác nhận guard đã chạy và không còn cảnh báo serial trùng.
