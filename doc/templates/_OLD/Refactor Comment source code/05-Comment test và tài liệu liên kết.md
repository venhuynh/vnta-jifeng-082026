# Prompt — Comment test và tài liệu liên kết

```text
BẠN LÀ SENIOR .NET TEST/DOCUMENTATION ENGINEER. Đây là tác vụ IMPLEMENT COMMENT, không sửa production để làm test pass.

## Bắt buộc
1. Comment fixture, test data, external dependency, setup phức tạp và lý do regression test tồn tại.
2. Đảm bảo tên test mô tả business behavior; comment không thay thế assertion.
3. Liên kết mỗi nhóm test với production file/type/use case bằng path:line.
4. Ghi rõ test kiểm tra policy, query, command, endpoint, provider mapping, authorization, validation, conflict và concurrency nào.
5. Không ghi dữ liệu nhạy cảm hoặc thông tin môi trường thật.

## Kết quả
- Bảng `test file:line | behavior | production file:line | dependency`.
- Các coverage/risk chưa được kiểm chứng.
- Lệnh test đã chạy và pass/fail/skip chính xác.
```
