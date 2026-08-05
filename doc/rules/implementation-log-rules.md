# Quy Tắc Nhật Ký Triển Khai

Áp dụng sau mỗi lượt AI code, chỉnh tài liệu hoặc thay đổi cấu hình.

## 1. Bắt buộc cập nhật nhật ký

- Sau khi hoàn tất thay đổi, AI phải cập nhật file log của đúng ngày và đúng nhánh trong `doc/implementation-log/`.
- Không để thay đổi code hoặc tài liệu kết thúc mà không có entry nhật ký tương ứng.
- Nhật ký phải được cập nhật trong cùng lượt làm việc.

## 2. Cấu trúc thư mục bắt buộc

- Không dùng lại file monolith `doc/implementation-log.md`.
- Nhật ký triển khai nằm trong thư mục `doc/implementation-log/`.
- Mỗi tổ hợp ngày làm việc và nhánh dùng một file theo format `yyyyMMdd-<ten-nhanh-da-chuan-hoa>.md`.
- `<ten-nhanh-da-chuan-hoa>` lấy từ `git branch --show-current`, chuyển về chữ thường, thay `/`, `\\`, khoảng trắng và mọi ký tự không phải `a-z`/`0-9` bằng `-`, gộp dấu `-` liên tiếp và bỏ dấu `-` đầu/cuối.
- Không dùng tên `main`, `master`, `develop` hoặc tên branch mơ hồ cho công việc trên feature branch. Nếu đang detached HEAD, dừng và xác định branch trước khi ghi log.
- Ví dụ:
  - branch `security/backend-security-review` -> `doc/implementation-log/20260717-security-backend-security-review.md`
  - branch `refactor/phu-cap-tong-hop-ui-20260717` -> `doc/implementation-log/20260717-refactor-phu-cap-tong-hop-ui-20260717.md`
- Các file lịch sử chỉ dùng ngày, như `20260717.md`, được giữ nguyên và không đổi tên hàng loạt.
- File index của khu vực log là `doc/implementation-log/index.md`.

## 3. Chọn file log đúng ngày và nhánh

- Ngày trong tên file là ngày làm việc thực tế theo timezone hiện hành của repo hoặc phiên làm việc.
- Trước khi ghi, chạy `git branch --show-current` và tạo tên file đã chuẩn hóa theo quy tắc trên.
- Nếu file của đúng ngày và đúng nhánh chưa tồn tại, phải tạo mới.
- Nếu file của đúng ngày và đúng nhánh đã tồn tại, thêm entry mới lên đầu file đó.
- Không ghi tiếp vào file cùng ngày của nhánh khác; đây là yêu cầu để giảm conflict khi merge PR.

## 4. Nội dung tối thiểu của mỗi entry

Mỗi entry cần có:

- Tác nhân thực hiện.
- Nội dung thay đổi.
- File hoặc thư mục liên quan.
- Cách kiểm chứng.
- Ghi chú nếu chưa chạy build hoặc test.

## 5. Cách ghi

- Entry mới nhất đặt lên đầu file của đúng ngày và đúng nhánh.
- Viết ngắn gọn, rõ việc đã làm.
- Không ghi thông tin nhạy cảm, secret, token, connection string hoặc dữ liệu cá nhân.
- Khi tạo file log mới, phải cập nhật `doc/implementation-log/index.md` với ngày và tên nhánh.

## 6. Đường dẫn phải phản ánh source đang hoạt động

- Entry mới phải ưu tiên đường dẫn trong `src/Vnta.HRM2026/...` nếu thay đổi xảy ra ở source hiện hành.
- Nếu cần nhắc lại file trong `src/Vnta.HRM/...`, phải ghi rõ đó là đường dẫn lịch sử từ source cũ hoặc từ tài liệu cũ.
- Không để implementation log mới tạo cảm giác `src/Vnta.HRM` vẫn là source chính.


