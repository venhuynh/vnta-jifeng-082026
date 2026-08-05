# Quy Tắc Kiểm Chứng

Áp dụng khi cần xác nhận một thay đổi đã đúng.

## 1. Build bắt buộc sau thay đổi source

- Sau mọi thay đổi source code, chạy `dotnet build`, `npm build` hoặc lệnh build tương đương cho toàn bộ project bị ảnh hưởng.
- Nếu build lỗi, tiếp tục sửa lỗi và build lại cho đến khi không còn lỗi compile trước khi báo cáo hoàn tất.
- Khi thay đổi đi qua nhiều layer, endpoint hoặc contract dùng chung, ưu tiên build solution để phát hiện lỗi wiring xuyên project.

## 2. Các kiểm tra được ưu tiên khi chưa build

- Đọc lại file đã sửa.
- Kiểm tra đường dẫn, tên file và cấu trúc thư mục.
- Kiểm tra JSON, Markdown hoặc cấu hình bằng mắt và bằng lệnh đọc file.
- Đối chiếu với quy tắc trong `doc/rules/`.
- Nếu thay đổi form DevExpress, đối chiếu
  `doc/rules/devexpress-input-validation-rules.md`: đúng edit context, validator,
  binding, message không lặp, save bị cancel khi fail và backend validate lại.

## 3. Báo cáo trung thực

- Phân biệt rõ "đã kiểm tra bằng đọc file" và "đã kiểm tra bằng build/test".
- Nếu chưa chạy build, phải nói rõ chưa chạy build.
- Không dùng cụm từ gây hiểu nhầm như "đã pass" khi chưa có bằng chứng thực thi.

## 4. Smoke test nhiều tab là bắt buộc với màn có callback nền

Áp dụng cho màn có:

- timer
- SignalR
- auto-refresh
- async detail load
- grid hoặc list cập nhật thường xuyên

Yêu cầu tối thiểu:

- mở nhiều tab cùng màn
- để callback chạy chồng trong một khoảng đủ dài
- thao tác qua lại giữa các tab
- xác nhận không có lỗi render component, không có callback muộn chạm state cũ và cảm giác tương tác vẫn mượt

Nếu chưa chạy smoke test kiểu này, phải báo rõ là chưa kiểm chứng render stability nhiều tab.



