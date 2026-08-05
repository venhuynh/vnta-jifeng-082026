# Prompt — Khảo sát dependency và lập comment map

```text
BẠN LÀ .NET/BLAZOR SOFTWARE ARCHITECT. Đây là tác vụ PHÂN TÍCH, không sửa source.

## Đầu vào
- Feature group/name: [điền]
- UI root hoặc route: [điền]
- Phạm vi source: [feature/project/toàn repository]

## Bắt buộc
1. Đọc AGENTS.md, kiểm tra git status và không đụng thay đổi có sẵn.
2. Dùng rg truy vết route → component → event → state/model → provider → HTTP → endpoint → contract/use case → policy → infrastructure/EF/SQL → test.
3. Xác định chính xác file, type, method và dòng; không suy đoán quan hệ chưa xác minh.
4. Phân loại file: cần comment, đã đủ comment, generated/vendor, shared và ngoài phạm vi.
5. Đánh dấu logic cần giải thích: nghiệp vụ, mapping, lifecycle, authorization, transaction, concurrency, cancellation, error và workaround.

## Kết quả bắt buộc (tiếng Việt)
- Dependency map dạng bảng với file:line ở mọi mắt xích.
- Bảng `File | type/method | đoạn cần comment | lý do | mức độ P0/P1/P2`.
- Danh sách XML docs cần bổ sung và comment Razor/C# cần bổ sung.
- Các vùng không thể xác minh và câu hỏi cần làm rõ.
```
