# Quy Tắc Validation Cho Edit Form

Áp dụng cho popup form, edit form và các luồng `create/update` trong HRM
Blazor.

## Mục tiêu

- Ngăn save logic bị rải rác thành các check ad hoc.
- Giữ validation hiển thị nhất quán giữa các popup form.
- Tách rõ trách nhiệm giữa form UI, page owner và service.

## 1. Cấu trúc bắt buộc

Mỗi form chỉnh sửa thực chiến nên có:

- component form riêng như `*EditForm.razor`
- `*.razor.cs` cho logic UI và event handler
- `*.razor.css` cho style scoped

Form component chỉ tập trung vào:

- render field
- render validation summary
- bind editor

Page hoặc owner component chịu trách nhiệm:

- tạo edit model
- cấp option list
- validate rule nghiệp vụ
- gọi service lưu dữ liệu

## 2. Model không được null

- Form phải nhận `Model` không null từ parent.
- Không để form tự âm thầm tạo model rỗng theo nhiều nhánh khó kiểm soát.
- Khi cần reset, parent là nơi quyết định lifecycle của model.

## 3. Validation phải hiện trong form

Bắt buộc:

- có `ValidationMessage` gần field hoặc `ValidationSummary` trong form body
- không hiển thị lặp cùng một lỗi ở nhiều vị trí nếu làm người dùng hiểu là có
  nhiều lỗi khác nhau
- lỗi validation không chỉ hiển thị bằng toast ngoài popup

Người dùng phải thấy và sửa lỗi ngay trong form đang mở.

## 4. Editor custom phải bind đúng validation pipeline

Áp dụng chi tiết theo
[`devexpress-input-validation-rules.md`](./devexpress-input-validation-rules.md).

Với editor DevExpress như `DxComboBox`, `DxDateEdit`, `DxCheckBox`:

- ưu tiên `@bind-Value`, `@bind-Date`, `@bind-Checked` hoặc `@bind-*` tương ứng
- chỉ dùng `Value`, `ValueChanged`, `ValueExpression` khi cần handler tùy chỉnh
- binding phải trỏ đúng property có DataAnnotations

Nếu editor nằm ngoài validation pipeline, phải tắt validation tường minh để
không hiển thị trạng thái gây hiểu nhầm.

## 5. Thay đổi field phụ thuộc phải qua setter method

Không cập nhật field liên quan bằng mutation rải rác trong markup.

Ưu tiên:

- `SetParent(...)`
- `SetDepartment(...)`
- `SetBranch(...)`
- `SetStatus(...)`

Điều này đặc biệt quan trọng với:

- parent-child hierarchy
- lookup phụ thuộc
- status kéo theo effective field khác

## 6. Save pipeline chuẩn

Trước khi gọi `SaveAsync`, `CreateAsync` hoặc `UpdateAsync`, phải theo thứ tự:

1. chuẩn hóa edit model
2. đồng bộ state phụ thuộc nếu cần
3. validate rule nghiệp vụ
4. nếu fail thì cancel save và đưa lỗi vào form
5. chỉ gọi service sau khi pass validation

## 7. Save handler phải chặn persistence khi fail

Trong handler như:

- `Grid_EditModelSaving`
- `TreeList_EditModelSaving`
- dialog save command

phải:

- xem edit model là single source of truth
- check rule required nếu business rule chưa được data annotation bao phủ đủ
- check rule quan hệ như self-parent, circular parent, parent không hợp lệ
- dừng save nếu lỗi

Với DevExpress event có cờ `e.Cancel`, phải set rõ `e.Cancel = true`.

## 8. Tách trách nhiệm rõ ràng

`*EditForm.razor`

- render field
- render lỗi
- hiển thị editor

Page owner hoặc `.razor.cs`

- build model
- tính option list
- orchestration save
- validation UI-level và screen-level

Application service

- persistence
- domain rule hoặc backend rule
- authorization thực thi

## 9. Quy tắc cho popup edit form của grid và tree list

- Nếu dùng popup edit form chuẩn của DevExpress, owner phải điều khiển popup
  qua grid hoặc tree list API, không dựng luồng chỉnh sửa thứ hai song song.
- Khi popup không mở, kiểm tra theo thứ tự:
  1. event toolbar có chạy không
  2. grid hoặc tree list có vào edit mode không
  3. `EditFormTemplate` có throw runtime exception không
  4. layout cha có chặn overlay bằng `overflow` không
- Không kết luận vội là DevExpress lỗi khi chưa kiểm tra host layout và
  validation pipeline.

## 10. Rule hiển thị lỗi

- Lỗi required: hiển thị gần field hoặc trong summary.
- Lỗi quan hệ nghiệp vụ: hiển thị trong form và chặn save.
- Lỗi save chung: hiển thị an toàn, ngắn gọn, đúng ngữ cảnh.
- Toast chỉ dùng bổ sung cho feedback hành động, không thay form validation.

## 11. Review checklist

- Có component form riêng.
- Có `ValidationMessage` gần field hoặc `ValidationSummary`.
- Không hiển thị lặp cùng một lỗi.
- Editor custom bind tường minh.
- Save handler chặn persistence khi validation fail.
- Quan hệ parent hoặc self hoặc circular đã được check trước save.
- Lỗi validation hiển thị trong form.
- Không gọi service lưu trước khi pass validation.
- Logic save không nằm inline trong `.razor`.
