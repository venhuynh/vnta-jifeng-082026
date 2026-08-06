# Quản lý template

## Prompt đang dùng

Chỉ dùng cặp thư mục prompt canonical sau cho refactor UI đến backend theo SOLID:

- Vnta-Blazor-2026/doc/templates/[TEMPLATE] refactor-solid-theo-chuan-phu-cap-chuyen-can/
- Vnta-Blazor-2026/doc/templates/[USAGE] refactor-solid-theo-chuan-phu-cap-chuyen-can/

Bắt đầu tại README.md trong thư mục `[TEMPLATE]`, điền Feature Refactor Manifest một lần, rồi chạy các prompt theo thứ tự. Thư mục `[USAGE]` được quản lý riêng cho việc sử dụng bộ prompt. Phụ cấp chuyên cần là chuẩn về architecture boundary và quality gate, không phải code nghiệp vụ để sao chép.

## Prompt lưu trữ

Toàn bộ prompt lịch sử đã được chuyển vào:

- Vnta-Blazor-2026/doc/templates/_OLD/

Chúng chỉ dùng để tra cứu lịch sử hoặc đối chiếu bối cảnh cũ. Không thêm prompt mới vào cấp gốc của doc/templates và không lấy prompt trong _OLD làm chuẩn cho feature mới.

## Mẫu tài liệu đang dùng

Các file sau không phải prompt và được giữ tại cấp gốc vì checklist/tài liệu hiện hành vẫn tham chiếu:

- adr-template.md
- feature-spec-template.md
- screen-implementation-template.md
- sprint-folder-template.md

## Quy tắc cập nhật

- Cập nhật hoặc bổ sung prompt refactor trong đúng thư mục `[TEMPLATE]` hoặc `[USAGE]` tương ứng.
- Chỉ di chuyển một prompt khỏi _OLD khi đã được rà soát, chuẩn hóa và thêm vào pack canonical.
- Mọi đường dẫn trong prompt phải bắt đầu từ Vnta-Blazor-2026.
- Khi thay đổi source/config theo một prompt, luôn ưu tiên AGENTS.md hiện hành về kiểm tra, commit và không push.
