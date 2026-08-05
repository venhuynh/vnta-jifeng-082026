# Lỗi validation của `DxTextBox` khi dùng one-way binding

## Ngày ghi nhận

- `2026-06-28`

## Màn hình hiện hành bị ảnh hưởng

- Menu: `Quản trị > Máy chấm công`
- Component hiện hành:
  - `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCong.razor`
  - `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCongEditForm.razor`

## Ghi chú lịch sử

- Tại thời điểm issue được ghi nhận, màn này còn dùng tên lịch sử `AttendanceDevices`.
- Một số log cũ hoặc commit cũ có thể vẫn nhắc tới:
  - `AttendanceDevices`
  - `AttendanceDeviceEditForm`
  - path cũ dưới `Components/Attendance/...`

## Triệu chứng

- Người dùng click nút `Mới`.
- Popup edit không mở được.
- UI không hiện lỗi rõ ràng nhưng log ghi exception khi render editor DevExpress.

## Root cause

Một số `DxTextBox` read-only trong form edit dùng:

- `Text="..."`
- `ReadOnly="true"`

nhưng không có:

- `ValidationEnabled="false"`

Khi đó DevExpress vẫn cố dựng validation metadata và throw lỗi vì thiếu `TextExpression`.

## Pattern sai

```razor
<DxTextBox Text="@Model.ActivationCode" ReadOnly="true" />
```

## Pattern đúng

```razor
<DxTextBox Text="@Model.ActivationCode"
           ReadOnly="true"
           ValidationEnabled="false" />
```

## Quy tắc áp dụng lại

- Field editable cần validation: dùng `@bind-Text`.
- Field read-only chỉ để hiển thị: dùng `Text=` và thêm `ValidationEnabled="false"`.
- Nếu gặp popup DevExpress không mở, kiểm tra log trước để tìm lỗi render component.

## File hiện hành nên kiểm tra khi lỗi tái diễn

- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCongEditForm.razor`
- `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/QuanTri/MayChamCong/MayChamCong.razor`

## Tài liệu liên quan

- `doc/troubleshooting/devexpress-textbox-quick-reference.md`
- `doc/rules/edit-form-validation-rules.md`
- `doc/project/source-map.md`


