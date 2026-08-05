# Quy Tắc AI Vibe Coding

Áp dụng khi AI tham gia phân tích, thiết kế, sửa code hoặc viết tài liệu trong dự án.

## 1. Đọc ngữ cảnh trước khi sửa

- Đọc file liên quan trước khi đề xuất hoặc chỉnh sửa.
- Ưu tiên quy ước đang có trong dự án hơn phong cách tự nghĩ ra.
- Không suy diễn nghiệp vụ HRM khi chưa có dữ liệu hoặc mô tả rõ.

## 2. Làm theo từng lát nhỏ

- Chia thay đổi thành phần nhỏ, dễ kiểm tra.
- Tránh sửa nhiều tầng cùng lúc nếu không cần.
- Không refactor lan rộng khi yêu cầu chỉ là sửa một chức năng.

## 3. Dùng nguồn chính thống

- Khi làm với DevExpress Blazor, ưu tiên tra MCP DevExpress hoặc tài liệu chính thức.
- Khi không chắc API, không tự đoán chữ ký hàm hoặc tên component.
- Ghi rõ giả định nếu phải tiếp tục trong lúc thiếu thông tin.
- Khi tạo hoặc sửa form nhập liệu DevExpress, bắt buộc đọc và áp dụng
  `doc/rules/devexpress-input-validation-rules.md`.
- Không kết thúc triển khai form nếu chưa kiểm tra edit context, binding,
  DataAnnotations, vị trí message, khả năng cancel save và validation backend.

## 4. Giao tiếp rõ ràng

- Báo ngắn gọn đang làm gì khi thay đổi tài liệu hoặc source.
- Nêu rủi rõ kỹ thuật nếu yêu cầu có thể gây lỗi, khó bảo trì hoặc lệch chuẩn Clean.
- Không nói đã kiểm chứng bằng build/test khi chưa thật sự chạy.
- Khi AI viết comment hoặc comment nhóm code, phải dùng tiếng Việt dễ hiểu cho dev và bám rule trong `code-rules.md`.

## 5. Tôn trọng giới hạn vận hành

- Không tự ý chạy build.
- Không tự ý thêm package.
- Không tự ý đổi kiến trúc.
- Không xóa code chưa hiểu rõ nguồn gốc.

## 5.1. Giữ cấu trúc C# dễ quét

- Khi AI tạo hoặc sửa file `.cs`, phải nhóm source code thành các khối rõ trách nhiệm như dependency, property, constructor, method public và helper private.
- Không trộn lẫn các nhóm method một cách ngẫu nhiên làm file khó đọc hoặc khó review.

## 6. Cập nhật nhật ký triển khai

- Sau khi AI code xong, phải chạy `git branch --show-current` và cập nhật file `doc/implementation-log/yyyyMMdd-<ten-nhanh-da-chuan-hoa>.md`.
- Nhật ký phải ghi rõ ngày, nội dung thay đổi, file liên quan và cách đã kiểm tra.
- Nếu ngày và nhánh hiện tại chưa có file log, phải tạo file mới theo đúng format trên; không thêm entry vào file cùng ngày của nhánh khác.
- Nếu không chạy build/test, phải ghi rõ là chưa chạy build/test.


