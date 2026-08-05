# Quy tắc Git workflow

Áp dụng cho mọi task có thay đổi trong repository.

## 1. Hoàn tất một task phải commit và push

- Codex và kỹ sư được phép chủ động commit rồi push sau mỗi task có thay đổi,
  không cần xin lại quyền riêng cho thao tác Git này.
- Sau khi hoàn tất phạm vi đã được giao và thực hiện kiểm chứng phù hợp, phải tạo một commit cho task đó.
- Ngay sau khi commit thành công, phải push commit lên remote của nhánh hiện tại.
- Một task chỉ được xem là đã bàn giao khi commit và push đều thành công.

## 2. Trình tự bắt buộc

1. Kiểm tra `git status` và `git diff` để xác nhận chỉ chứa thay đổi thuộc task.
2. Thực hiện kiểm chứng theo `verification-rules.md` và ghi rõ mức kiểm chứng đã thực hiện.
3. Commit với message ngắn, mô tả đúng thay đổi của task.
4. Push bằng fast-forward thông thường lên remote của nhánh hiện tại.
5. Báo lại hash commit và trạng thái push trong kết quả bàn giao.

## 3. An toàn và ngoại lệ

- Không gộp thay đổi không liên quan hoặc thay đổi chưa rõ chủ sở hữu vào commit của task.
- Không commit secret, connection string thật, file build hoặc file sinh tạm.
- Không dùng `push --force`, trừ khi người dùng yêu cầu rõ ràng.
- Chỉ push commit thuộc task đã hoàn tất; không push một worktree đang có phần
  refactor dở dang hoặc thay đổi ngoài phạm vi chưa được tách an toàn.
- Nếu kiểm chứng thất bại, push bị lỗi hoặc worktree có thay đổi ngoài phạm vi không thể tách an toàn, không được tuyên bố task hoàn tất; phải báo rõ nguyên nhân và trạng thái commit/push.
- Với task chỉ đọc, tư vấn hoặc báo cáo không tạo thay đổi repository, không cần tạo commit/push.
