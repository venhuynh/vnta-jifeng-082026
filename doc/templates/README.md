# Bộ Mẫu Tài Liệu

## Mục tiêu

Thư mục `doc/templates` chứa các mẫu tài liệu và prompt dùng lại cho nhiều nhánh triển khai khác nhau trong dự án.

Nguyên tắc chung:

- ưu tiên tái sử dụng mẫu thay vì viết lại từ đầu;
- mọi đường dẫn trong prompt phải bắt đầu từ `Vnta-Blazor-2026`;
- khi cập nhật mẫu, phải cập nhật trực tiếp đúng file nguồn thay vì tạo biến thể trùng lặp;
- nếu một bộ prompt đã được tách theo giai đoạn, không viết lại nội dung chi tiết vào file chỉ mục.

## Bộ Prompt Refactor Phụ Cấp

Bộ prompt refactor phụ cấp đã được tách thành các file riêng theo từng giai đoạn:

- `Vnta-Blazor-2026\doc\templates\prompt-refactor-phu-cap.txt`
  File chỉ mục, dùng để điều hướng nhanh đến từng giai đoạn.
- `Vnta-Blazor-2026\doc\templates\prompt-refactor-phu-cap-giai-doan-1.txt`
  Mở nhánh mới, nạp tài liệu, dựng context ban đầu.
- `Vnta-Blazor-2026\doc\templates\prompt-refactor-phu-cap-giai-doan-2.txt`
  Lập bản đồ logic xử lý và luồng dữ liệu hiện tại.
- `Vnta-Blazor-2026\doc\templates\prompt-refactor-phu-cap-giai-doan-3.txt`
  Refactor UI, state, popup, toast, icon, layout.
- `Vnta-Blazor-2026\doc\templates\prompt-refactor-phu-cap-giai-doan-4.txt`
  Chuẩn hóa logic code-behind.
- `Vnta-Blazor-2026\doc\templates\prompt-refactor-phu-cap-giai-doan-5.txt`
  Build, test, hoàn tất tài liệu, đối chiếu checklist và chuẩn bị commit.

## Thứ tự sử dụng khuyến nghị

1. Giai đoạn 1
2. Giai đoạn 2
3. Giai đoạn 3
4. Giai đoạn 4
5. Giai đoạn 5

Không bỏ qua giai đoạn giữa nếu màn hình chưa được phân tích và chuẩn hóa đầy đủ.

## Prompt chuẩn hóa folder xuyên project

- `Vnta-Blazor-2026\doc\templates\prompt-refactor-cau-truc-folder-xuyen-project.txt`
  Dùng cho Codex/AI agent refactor một context nghiệp vụ theo cấu trúc folder mới
  xuyên `Web.Client`, `Web`, `Application`, `Domain` và `Infrastructure`.
  Prompt bắt buộc đọc standard, lập mapping, giữ cross-cutting root, dùng `git mv`,
  cập nhật namespace/DI/tài liệu và chỉ kiểm tra tĩnh nếu chưa được yêu cầu build.

## Prompt refactor SOLID theo màn hình UI

- `Vnta-Blazor-2026\doc\templates\prompt-refactor-solid-ui-theo-man-hinh.txt`
  Nhận một đầu vào là path `.razor`, folder feature, route hoặc `ContextKey`; agent
  tự xác định page owner và child UI, lập boundary map, refactor theo SOLID, chạy
  kiểm chứng bắt buộc và cập nhật tài liệu. Dùng cho một UI/feature tại một thời điểm.

## Prompt sửa chiều cao Pager và CSS isolation

- `Vnta-Blazor-2026\doc\templates\prompt-fix-pager-height-css-isolation.txt`
  Dùng khi footer/pager của một màn hình Blazor bị cao bất thường hoặc để lại vùng
  trắng lớn bên dưới. Prompt yêu cầu đối chiếu với màn hình chuẩn, kiểm tra chuỗi
  flex từ root đến `DxGrid`, xử lý `::deep` ở đúng CSS owner, dùng `PageSize` khi
  có pager ngoài và kiểm chứng CSS isolation sau build.

## Quy ước đặt tên

Đối với file prompt tách giai đoạn:

- dùng chữ thường;
- dùng `kebab-case`;
- thêm hậu tố `giai-doan-N` để dễ tìm kiếm và sắp xếp.

Ví dụ:

- `prompt-refactor-phu-cap-giai-doan-1.txt`
- `prompt-refactor-phu-cap-giai-doan-2.txt`

## Quy tắc cập nhật

- Nếu chỉnh nội dung của một giai đoạn, chỉ chỉnh trong file giai đoạn tương ứng.
- Nếu đổi cấu trúc bộ prompt, phải cập nhật lại file chỉ mục `prompt-refactor-phu-cap.txt`.
- Nếu thay đổi có ảnh hưởng đến cách dùng chung, phải cập nhật file `README.md` này.

## Trạng thái hiện tại

Bộ prompt refactor phụ cấp hiện đã được:

- tách riêng theo từng giai đoạn;
- chuẩn hóa tên file;
- chuẩn hóa file chỉ mục;
- sẵn sàng bước sang giai đoạn hoàn tất tài liệu để commit.
