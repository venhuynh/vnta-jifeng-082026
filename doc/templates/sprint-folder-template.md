# Mẫu tạo folder sprint

Khi tạo sprint mới, dùng cấu trúc:

```text
doc/sprints/KhauTru/sprint-015-thue-tncn-ui/
  sprint-plan.md
  tasks.md
  implementation-notes.md
  review-notes.md
```

Quy tắc:

- Chọn đúng folder nhóm theo business domain chính của sprint.
- Mỗi sprint là một folder riêng tên `sprint-###-slug`.
- Không trộn task của sprint khác vào cùng một folder.
- Trước khi tạo sprint mới cho một màn hình đã tồn tại, rà soát sprint cũ của màn đó trong nhóm hiện hành hoặc trong `doc/sprints/_OLD/`, rồi ghi lại kết luận kế thừa trong `implementation-notes.md`.
- Sprint có form nhập liệu phải ghi validation trong plan, task, implementation notes và review notes theo `doc/rules/devexpress-input-validation-rules.md`.
- Nếu tạo nhóm sprint mới, thêm luôn `README.md` cho nhóm và cập nhật `doc/sprints/README.md`, `doc/sprints/index.md`.
- Cập nhật đúng file ngày và branch trong `doc/implementation-log/yyyyMMdd-<ten-nhanh-da-chuan-hoa>.md` sau khi tạo hoặc sửa sprint.
