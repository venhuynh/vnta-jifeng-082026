# Quy Tắc Validation Input DevExpress Toàn Ứng Dụng

Áp dụng bắt buộc cho mọi form nhập liệu DevExpress Blazor trong source hiện hành
`src/Vnta.HRM2026`.

Tài liệu chuẩn:

- DevExpress Blazor 26.1 - Validate Input:
  `https://docs.devexpress.com/Blazor/402066/components/data-editors/validate-input`

## 1. Nguyên tắc bắt buộc

- Validation UI phải dùng cơ chế validation chuẩn của Blazor dựa trên `EditContext`.
- Model nhập liệu phải khai báo DataAnnotations phù hợp như `Required`,
  `StringLength`, `Range`, `RegularExpression` hoặc validation attribute riêng.
- Form độc lập phải có `DataAnnotationsValidator`.
- Không được chỉ dựa vào validation phía client để bảo vệ dữ liệu. Service hoặc
  backend phải validate lại trước persistence.
- Thông báo hiển thị cho người dùng phải là tiếng Việt có dấu.

## 2. EditForm và EditContext

Với form độc lập:

- Dùng `EditForm` với `Model` hoặc `EditContext`.
- Không truyền đồng thời cả `Model` và `EditContext`.
- Dùng `OnValidSubmit` và `OnInvalidSubmit` khi form tự sở hữu nút submit.

Với `DxGrid` hoặc `DxTreeList` popup edit:

- Dùng `EditContext` do `GridEditFormTemplateContext` hoặc
  `TreeListEditFormTemplateContext` cung cấp.
- Không lồng thêm một `EditForm` thứ hai bên trong popup edit.
- Không tự thêm `DataAnnotationsValidator` trong `EditFormTemplate` của
  `DxGrid` hoặc `DxTreeList` vì các component này đã dùng DataAnnotationsValidator
  mặc định cho edit model.
- Cascade edit context cho `ValidationMessage` và editor DevExpress trong form
  component khi form được tách thành component con.
- Bật `ValidationEnabled="true"` ở grid hoặc tree list khi có chỉnh sửa dữ liệu.
- Nếu khai báo `CustomValidators`, lưu ý custom validators override validator
  mặc định; chỉ khi đó mới khai báo lại `DataAnnotationsValidator` trong
  `CustomValidators` nếu vẫn cần DataAnnotations.

## 3. Binding editor

- Editor tham gia validation phải dùng two-way binding tới đúng property model:
  `@bind-Text`, `@bind-Value`, `@bind-Date`, `@bind-Checked` hoặc binding tương
  đương mà component hỗ trợ.
- `@bind-*` là lựa chọn mặc định vì Blazor tự tạo value expression cho
  validation.
- Chỉ dùng bộ ba `Value`, `ValueChanged`, `ValueExpression` khi cần handler tùy
  chỉnh hoặc cần cần thiệp state phụ thuộc.
- Không bind editor vào property trung gian nếu DataAnnotations nằm trên một
  property khác.

## 4. Hiển thị kết quả validation

Mỗi form phải có:

- validation icon hoặc colored outline của editor
- `ValidationMessage` gần từng field quan trọng hoặc `ValidationSummary` để tổng
  hợp lỗi của toàn form
- không hiển thị cùng một lỗi vừa cạnh field vừa trong summary nếu điều đó làm
  người dùng thấy lỗi bị lặp

Chuẩn mặc định của ứng dụng:

- Editor editable đặt `ShowValidationIcon="true"`.
- Các editor trong cùng form phải dùng `ShowValidationSuccessState` nhất quán.
- Message lỗi phải nằm ngay dưới editor và không làm text chồng lấn.
- Message lỗi phải dùng màu danger đỏ rõ ràng; không chỉ dựa vào border hoặc icon của editor
  để biểu thị trạng thái lỗi.
- Popup form nhập liệu ngắn ưu tiên `ValidationMessage` cạnh field thay vì
  `ValidationSummary`.
- Không dùng toast làm kênh duy nhất cho lỗi nhập liệu theo field.

## 5. Custom validation và lỗi backend

- Validation quan hệ nhiều field có thể dùng `IValidatableObject`, custom
  `ValidationAttribute` hoặc validator component với `ValidationMessageStore`.
- Lỗi backend gắn được với field phải đưa vào edit context để
  `ValidationMessage` hiển thị đúng field.
- Lỗi nghiệp vụ hoặc persistence không gắn với field phải hiển thị qua shared
  `IHrmToastService`.
- Khi validation hoặc persistence thất bại trong grid/tree save event, phải đặt
  `e.Cancel = true`.
- Chỉ gọi persistence sau khi UI validation và business validation đều pass.

## 6. Editor read-only

- Nếu editor read-only chỉ hiển thị dữ liệu bằng one-way binding và không tham gia
  validation, đặt `ValidationEnabled="false"`.
- Không bật validation UI cho field không thể sửa.
- Nếu field read-only vẫn thuộc validation workflow, phải dùng binding có value
  expression hợp lệ.

## 7. Save pipeline

Thứ tự bắt buộc:

1. chuẩn hóa edit model
2. đồng bộ field phụ thuộc
3. chạy validation model
4. chạy validation nghiệp vụ/backend
5. cancel và hiển thị lỗi nếu fail
6. gọi persistence
7. hiển thị toast success hoặc error

## 8. Mẫu form độc lập

```razor
<EditForm Model="@Model"
          OnValidSubmit="@HandleValidSubmit"
          OnInvalidSubmit="@HandleInvalidSubmit">
    <DataAnnotationsValidator />

    <DxTextBox @bind-Text="Model.Name"
               ShowValidationIcon="true" />
    <ValidationMessage For="@(() => Model.Name)" />

    <DxButton Text="Lưu" SubmitFormOnClick="true" />
</EditForm>
```

## 9. Mẫu popup Grid hoặc TreeList

```razor
<CascadingValue Value="EditFormContext.EditContext">
    <DxTextBox @bind-Text="Model.Name"
               ShowValidationIcon="true" />
    <ValidationMessage For="@(() => Model.Name)" />
</CascadingValue>
```

## 10. Checklist review

- Model có DataAnnotations đúng rule nghiệp vụ cơ bản.
- Form dùng đúng một edit context.
- Form độc lập có `DataAnnotationsValidator`; popup Grid/TreeList dùng validator
  mặc định của component.
- Editor editable dùng two-way binding đúng property.
- Có validation icon hoặc outline.
- Có message gần field hoặc summary toàn form, không lặp cùng một lỗi và message lỗi dùng
  màu danger đỏ rõ ràng.
- Grid hoặc tree edit bật validation.
- Editor read-only không tham gia validation đã tắt validation tường minh.
- Save bị cancel khi validation fail.
- Backend validate lại trước persistence.
- Toast dùng cho feedback hành động và lỗi nghiệp vụ/persistence, không thay thế
  field validation.

