# Quy tắc tài liệu sprint

Áp dụng khi triển khai chức năng theo sprint.

## 1. Vị trí sprint

- Tất cả tài liệu sprint phải nằm trong `doc/sprints/`.
- Sprint mới phải đặt theo dạng `doc/sprints/<nhom>/sprint-###-slug/`.
- Không tạo sprint mới trực tiếp dưới root `doc/sprints/`.
- Các folder bắt đầu bằng `_` là folder hệ thống như `_template`, `_OLD`; không dùng các folder này để đặt sprint active mới.

## 2. Chọn nhóm sprint

- Chọn nhóm theo root menu hoặc business domain chính của màn hình hoặc tính năng.
- Nếu một hạng mục chạm nhiều khu vực, đặt sprint ở nhóm chịu trách nhiệm chính và ghi rõ phần cross-domain trong `sprint-plan.md`.
- Nếu chưa có nhóm phù hợp, tạo folder nhóm mới kèm `README.md` mô tả phạm vi trước khi thêm sprint đầu tiên.

## 3. Cấu trúc sprint khuyến nghị

```text
doc/sprints/KhauTru/sprint-015-thue-tncn-ui/
  sprint-plan.md
  tasks.md
  implementation-notes.md
  review-notes.md
```

## 4. Cập nhật chỉ mục sprint

- Khi tạo nhóm mới, cập nhật `doc/sprints/README.md` và `doc/sprints/index.md`.
- Khi tạo sprint mới trong nhóm hiện có, cập nhật tối thiểu `README.md` của nhóm nếu nhóm đó đang được dùng như điểm vào chính.
- Nếu sprint cũ được archive sang `_OLD`, cập nhật lại chỉ mục chính để tránh link chết.

## 5. Nội dung tối thiểu

Mỗi sprint nên có:

- mục tiêu sprint
- phạm vi công việc
- danh sách task
- ghi chú triển khai
- kết quả kiểm chứng
- việc còn lại hoặc rủi ro

Nếu sprint có form nhập liệu, tài liệu phải ghi rõ validation model, validation nghiệp vụ hoặc backend, cách hiển thị lỗi và rule `doc/rules/devexpress-input-validation-rules.md` được áp dụng.

## 5.1. Nội dung validation bắt buộc

Với sprint có create, update, import hoặc form nhập liệu:

- `sprint-plan.md` phải nêu field bắt buộc, giới hạn và rule quan hệ.
- `tasks.md` phải có task triển khai và kiểm tra validation.
- `implementation-notes.md` phải ghi edit context, validator, binding, message và save pipeline đã dùng.
- `review-notes.md` phải xác nhận không lưu khi validation fail, không lặp message và backend validate lại trước persistence.

## 6. Cập nhật nhật ký triển khai

- Khi tạo hoặc cập nhật sprint, phải ghi thêm vào file theo ngày và nhánh hiện tại: `doc/implementation-log/yyyyMMdd-<ten-nhanh-da-chuan-hoa>.md`.
- Không dùng file log cùng ngày của branch khác; xem quy tắc chuẩn hóa tên branch tại `doc/rules/implementation-log-rules.md`.
- Entry nhật ký phải nêu rõ sprint nào hoặc nhóm sprint nào được tạo hoặc thay đổi.

## 7. Rà soát sprint theo màn hình trước khi triển khai tiếp

- Khi tiếp tục làm một màn hình đã có tài liệu sprint trước đó, phải rà soát lại folder sprint tương ứng trước khi code.
- Nếu có sprint mới cho cùng màn hình, `implementation-notes.md` của sprint mới phải ghi rõ đã đọc sprint nào trước đó và kết luận gì được kế thừa.
- Sprint cũ liên quan đến cùng màn hình phải được cập nhật tối thiểu ở `implementation-notes.md` hoặc `review-notes.md` để ghi rõ trạng thái mới, giả định không còn đúng hoặc hướng refactor tiếp theo.
- Nếu thay đổi làm đổi boundary dữ liệu, route, source folder hoặc nghiệp vụ của màn hình, cần cập nhật cả sprint folder và screen spec liên quan thay vì chỉ sửa code.
