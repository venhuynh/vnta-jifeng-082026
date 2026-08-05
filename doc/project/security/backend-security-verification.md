# Kiểm Chứng Refactor Bảo Mật Backend

## Checklist nghiệm thu

- [ ] Endpoint và service kiểm tra xác thực/phân quyền theo policy phù hợp.
- [ ] API kiểm tra ownership và phạm vi dữ liệu, không chỉ tin vào ID do client gửi.
- [ ] Input được validate ở server trước persistence; truy vấn không ghép chuỗi từ input.
- [ ] Lỗi trả về client không lộ stack trace, secret, schema hoặc dữ liệu nhân sự.
- [ ] Log/audit không chứa mật khẩu, token, connection string hoặc PII không cần thiết.
- [ ] Secret và cấu hình production không nằm trong source, tài liệu hoặc test artifact.
- [ ] Credential cũ đã bị revoke sau khi runtime dùng secret mới.
- [ ] Gateway không có mTLS/HMAC hợp lệ hoặc dùng nonce replay bị từ chối.
- [ ] Thay đổi schema có migration được review; DB constraint hỗ trợ quy tắc toàn vẹn quan trọng.
- [ ] Dependency và container/package liên quan đã được rà soát theo phạm vi thay đổi.
- [ ] Kiểm thử tái lập được xác nhận phát hiện đã không còn khai thác được.
- [ ] Sổ phát hiện, sprint và implementation log đã phản ánh cùng trạng thái.

## Bằng chứng tối thiểu khi đóng phát hiện

- ID phát hiện và commit/PR xử lý.
- Test hoặc bước tái lập trước/sau đã được làm sạch dữ liệu nhạy cảm.
- Phạm vi regression đã kiểm tra và giới hạn còn lại.
- Người review, ngày kiểm chứng và quyết định trạng thái.
