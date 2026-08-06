# Prompt — Kiểm định comment và hoàn tất

```text
BẠN LÀ PRINCIPAL ENGINEER REVIEWER. Hãy review comment/documentation đã bổ sung cho feature; chỉ sửa comment sai hoặc thiếu, không sửa production logic.

## Bắt buộc
1. Đọc git diff và xác nhận thay đổi chỉ là comment/XML docs.
2. Kiểm tra mọi liên kết UI → provider → API → application → infrastructure → test có file path và line reference tồn tại.
3. Tìm comment hiển nhiên, sai, lỗi thời, trùng lặp, TODO không có ngữ cảnh, secret và comment-out code.
4. Kiểm tra XML docs có khớp nullability, parameter, return, exception và behavior thật.
5. Build/analyzer/test project liên quan; không che giấu lỗi có sẵn.

## Báo cáo cuối cùng (tiếng Việt)
- Phạm vi/file đã kiểm định.
- Comment đạt/chưa đạt theo UI, API, Application, Infrastructure, Database và Test.
- Các liên kết file:line đã xác minh.
- Comment đã sửa/xóa và lý do.
- Xác nhận behavior, API, schema, route, DI và security không thay đổi.
- Lệnh kiểm tra và kết quả; backlog tài liệu còn lại theo P0/P1/P2.
```
