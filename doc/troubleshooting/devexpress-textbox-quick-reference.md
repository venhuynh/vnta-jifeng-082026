# DevExpress Blazor - Quick Reference cho TextBox và Validation

## 📖 Tổng quan

File này cung cấp quick reference về cách sử dụng đúng `DxTextBox` và các data editor khác trong DevExpress Blazor, đặc biệt về validation và binding.

## 🎯 Quy tắc vàng

### ✅ ĐÚNG: Trường editable với validation

```razor
<DxTextBox @bind-Text="Model.Code" 
		   ShowValidationIcon="true" 
		   ShowValidationSuccessState="true" />
```

**Khi nào dùng:**
- Trường người dùng có thể chỉnh sửa
- Cần validation (Required, MaxLength, etc.)
- Muốn hiển thị validation state

**Lưu ý:**
- Phải dùng `@bind-Text` (two-way binding)
- Framework tự động tạo `TextExpression`
- Model property cần có validation attributes

### ✅ ĐÚNG: Trường read-only chỉ hiển thị

```razor
<DxTextBox Text="@Model.MacAddress" 
		   ReadOnly="true" 
		   ValidationEnabled="false" />
```

**Khi nào dùng:**
- Trường chỉ hiển thị, không chỉnh sửa
- Data từ server hoặc tính toán
- Không cần validation

**Lưu ý:**
- Dùng `Text=` (one-way binding)
- **BẮT BUỘC** phải có `ValidationEnabled="false"`
- Không cần validation attributes trên Model

### ✅ ĐÚNG: Trường hiển thị formatted text

```razor
<DxTextBox Text="@FormatDateTime(Model.LastRequestTime)" 
		   ReadOnly="true" 
		   ValidationEnabled="false" />
```

**Khi nào dùng:**
- Hiển thị giá trị đã format
- Text không liên kết trực tiếp với Model property
- Chỉ đọc

**Lưu ý:**
- Text có thể là method result, computed property
- **BẮT BUỘC** có `ValidationEnabled="false"`

### ❌ SAI: Read-only không tắt validation

```razor
<!-- ❌ LỖI: Thiếu ValidationEnabled="false" -->
<DxTextBox Text="@Model.MacAddress" ReadOnly="true" />
```

**Lỗi:**
```
DevExpress.Blazor.Internal.Editors.Models.TextBoxModel requires a value for the 'TextExpression' property.
```

**Cách fix:**
```razor
<!-- ✅ Thêm ValidationEnabled="false" -->
<DxTextBox Text="@Model.MacAddress" 
		   ReadOnly="true" 
		   ValidationEnabled="false" />
```

## 📊 Bảng tham chiếu nhanh

| Trường hợp | Binding | ReadOnly | ValidationEnabled | Validation Attrs | Ghi chú |
|------------|---------|----------|-------------------|------------------|---------|
| **Editable + Validation** | `@bind-Text` | `false` | `true` (default) | ✅ Cần | Trường nhập liệu thông thường |
| **Editable + No Validation** | `@bind-Text` | `false` | `false` | ❌ Không | Hiếm khi dùng |
| **Read-only Display** | `Text=` | `true` | **`false` (BẮT BUỘC)** | ❌ Không | Hiển thị data từ server |
| **Formatted Display** | `Text=` | `true` | **`false` (BẮT BUỘC)** | ❌ Không | Hiển thị text đã format |
| **Read-only + Validation** | `@bind-Text` | `true` | `true` (default) | ✅ Cần | Hiếm khi dùng |

## 🔧 Áp dụng cho các component khác

### DxMemo (Multi-line text)

```razor
<!-- ✅ Editable -->
<DxMemo @bind-Text="Model.Description" />

<!-- ✅ Read-only -->
<DxMemo Text="@Model.Notes" 
		ReadOnly="true" 
		ValidationEnabled="false" />
```

### DxSpinEdit (Number input)

```razor
<!-- ✅ Editable -->
<DxSpinEdit @bind-Value="Model.Port" />

<!-- ✅ Read-only -->
<DxSpinEdit Value="@Model.UserCount" 
			ReadOnly="true" />
```

**Lưu ý:** `DxSpinEdit` với `@bind-Value` ít khi gặp lỗi validation như TextBox.

### DxDateEdit (Date picker)

```razor
<!-- ✅ Editable -->
<DxDateEdit @bind-Date="Model.BirthDate" />

<!-- ✅ Read-only -->
<DxDateEdit Date="@Model.CreatedDate" 
			ReadOnly="true" />
```

### DxComboBox (Dropdown)

```razor
<!-- ✅ Editable -->
<DxComboBox Data="@DataSource" 
			@bind-Value="Model.CategoryId" 
			TextFieldName="Name" 
			ValueFieldName="Id" />

<!-- ✅ Read-only display -->
<DxTextBox Text="@Model.CategoryName" 
		   ReadOnly="true" 
		   ValidationEnabled="false" />
```

## 🎓 Best Practices

### 1. Luôn review read-only fields

Khi tạo form mới, hãy check tất cả read-only fields:

```bash
# Tìm các DxTextBox có ReadOnly="true"
grep -r 'ReadOnly="true"' --include="*.razor"

# Kiểm tra xem có ValidationEnabled="false" hay chưa
```

### 2. Template cho các trường hợp thông dụng

#### Form với mix editable và read-only:

```razor
<DxFormLayout>
	<!-- Editable field -->
	<DxFormLayoutItem Caption="Mã thiết bị">
		<DxTextBox @bind-Text="Model.Code" 
				   ShowValidationIcon="true" />
	</DxFormLayoutItem>

	<!-- Read-only field -->
	<DxFormLayoutItem Caption="IP Address">
		<DxTextBox Text="@Model.IpAddress" 
				   ReadOnly="true" 
				   ValidationEnabled="false" />
	</DxFormLayoutItem>

	<!-- Formatted display -->
	<DxFormLayoutItem Caption="Lần kết nối cuối">
		<DxTextBox Text="@FormatDateTime(Model.LastConnection)" 
				   ReadOnly="true" 
				   ValidationEnabled="false" />
	</DxFormLayoutItem>
</DxFormLayout>
```

### 3. Validation attributes trên Model

```csharp
public class DeviceEditModel
{
	// ✅ Editable field - Cần validation attributes
	[Required(ErrorMessage = "Mã thiết bị là bắt buộc")]
	[MaxLength(50, ErrorMessage = "Mã thiết bị tối đa 50 ký tự")]
	public string Code { get; set; } = string.Empty;

	// ✅ Read-only field - KHÔNG cần validation attributes
	// Vì sẽ hiển thị với ValidationEnabled="false"
	public string? IpAddress { get; set; }

	public string? MacAddress { get; set; }

	public DateTime? LastConnection { get; set; }
}
```

### 4. Helper methods cho formatting

```csharp
@code {
	// Helper để format datetime cho read-only display
	private string FormatDateTime(DateTime? value)
	{
		return value.HasValue 
			? value.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
			: "Chưa có dữ liệu";
	}

	// Helper để format số
	private string FormatNumber(int? value)
	{
		return value?.ToString("N0") ?? "0";
	}
}
```

## 🐛 Troubleshooting

### Lỗi: "TextExpression property required"

**Triệu chứng:**
```
DevExpress.Blazor.Internal.Editors.Models.TextBoxModel requires a value for the 'TextExpression' property.
```

**Nguyên nhân:**
- TextBox dùng `Text=` (one-way binding)
- Không có `ValidationEnabled="false"`

**Giải pháp:**
```razor
<!-- ❌ Lỗi -->
<DxTextBox Text="@Model.Value" ReadOnly="true" />

<!-- ✅ Fix -->
<DxTextBox Text="@Model.Value" 
		   ReadOnly="true" 
		   ValidationEnabled="false" />
```

### Popup/Dialog không mở được

**Các bước kiểm tra:**
1. Kiểm tra log Serilog: `Logs/jifeng-hrm/application-*.log`
2. Tìm "Unhandled exception rendering component"
3. Kiểm tra tất cả read-only TextBox có `ValidationEnabled="false"`
4. Build lại project sau khi fix

### Validation không hoạt động

**Kiểm tra:**
1. Đang dùng `@bind-Text` (không phải `Text=`)?
2. Model property có validation attributes?
3. ValidationEnabled không bị set thành `false`?
4. EditContext được thiết lập đúng?

## 📚 Tài liệu tham khảo

### DevExpress Official Docs:
- [DxTextBox Documentation](https://docs.devexpress.com/Blazor/DevExpress.Blazor.DxTextBox)
- [Data Editors - Binding](https://docs.devexpress.com/Blazor/401330/data-editors#binding)
- [Data Editors - Validation](https://docs.devexpress.com/Blazor/401330/data-editors#validation)
- [Edit Forms in Grid](https://docs.devexpress.com/Blazor/403454/grid/edit-data-and-validate-input)

### Microsoft Docs:
- [Blazor Data Binding](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/data-binding)
- [Blazor Forms and Validation](https://learn.microsoft.com/en-us/aspnet/core/blazor/forms/)

### Internal Docs:
- `doc/troubleshooting/devexpress-textbox-validation-error.md` - Chi tiết bug fix case study

## 📝 Checklist khi tạo form mới

- [ ] Xác định các trường editable vs read-only
- [ ] Editable fields: Dùng `@bind-Text` + validation attributes
- [ ] Read-only fields: Dùng `Text=` + `ReadOnly="true"` + `ValidationEnabled="false"`
- [ ] Formatted display: Tạo helper methods cho formatting
- [ ] Test popup/dialog mở được và hiển thị đúng
- [ ] Kiểm tra validation hoạt động cho editable fields
- [ ] Build thành công, không có warnings
- [ ] Test tạo mới và chỉnh sửa data

## ⚡ Quick Commands

```bash
# Tìm tất cả DxTextBox trong project
Get-ChildItem -Recurse -Filter "*.razor" | Select-String "DxTextBox"

# Tìm read-only TextBox có thể thiếu ValidationEnabled
Get-ChildItem -Recurse -Filter "*.razor" | Select-String 'ReadOnly="true"' -Context 0,1 | Select-String -NotMatch "ValidationEnabled"

# Xem log lỗi validation
Get-Content "Logs/jifeng-hrm/application-*.log" | Select-String "TextExpression|ValidationEnabled" -Context 2,2
```

## 🎯 TL;DR (Too Long; Didn't Read)

**Quy tắc đơn giản nhất:**

```razor
<!-- Người dùng nhập: Dùng @bind-Text -->
<DxTextBox @bind-Text="Model.EditableField" />

<!-- Chỉ hiển thị: Dùng Text= + ReadOnly + ValidationEnabled="false" -->
<DxTextBox Text="@Model.ReadOnlyField" 
		   ReadOnly="true" 
		   ValidationEnabled="false" />
```

**Nhớ:** Read-only + Text= → **BẮT BUỘC** ValidationEnabled="false" ✅


