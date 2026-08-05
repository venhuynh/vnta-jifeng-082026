# Quy Tắc Shared Toast Layer

Áp dụng cho mọi UI trong `src/Vnta.HRM2026` dùng DevExpress Blazor.

## 1. Mục tiêu

- Toàn bộ ứng dụng HRM chỉ có một toast layer dùng chung.
- Semantic `success`, `info`, `warning`, `error` phải thống nhất giữa các màn.
- Business page không phụ thuộc trực tiếp vào raw DevExpress toast API.

## 2. Thành phần chuẩn

- Host provider chuẩn: `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Shared/Feedback/HrmToastProvider.razor`
- Shared service chuẩn:
  - `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Services/Ui/IHrmToastService.cs`
  - `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Services/Ui/HrmToastService.cs`
- Default cấu hình chuẩn:
  - `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Services/Ui/HrmToastDefaults.cs`
- Điểm render provider chuẩn:
  - `src/Vnta.HRM2026/Vnta.Hrm.Web.Client/Components/Layout/MainLayout.razor`

## 3. Quy định bắt buộc

- Mọi toast phát sinh từ UI phải đi qua `IHrmToastService`.
- Chỉ `HrmToastProvider` được phép render `DxToastProvider`.
- Chỉ shared toast infrastructure được phép chạm trực tiếp vào `IToastNotificationService`, `ToastOptions`, `ShowToast(...)`, `CloseToast(...)`, `ProviderName` và `DxToastProvider`.
- Page nghiệp vụ, dashboard, popup nghiệp vụ, component con và demo screen đang sống trong app không được:
  - inject `IToastNotificationService`
  - gọi `ShowToast(...)` hoặc `CloseToast(...)`
  - render `DxToastProvider`
  - tự đặt `ProviderName`
- Khi cần custom `DisplayTime`, `RenderStyle`, `ThemeMode` hoặc action button hoặc template, vẫn phải cấu hình qua `IHrmToastService`, không được mở đường vòng bằng raw DevExpress API.
- Lỗi validation chặn thao tác phải hiển thị trong form hoặc gần vùng nhập liệu; toast chỉ là kênh bổ sung.

## 4. Semantic dùng chung

- `ShowSuccess(...)`: lưu, xóa, gửi, đồng bộ hoặc thao tác đã hoàn tất thành công.
- `ShowInfo(...)`: thông tin không chặn luồng, hướng dẫn nhẹ, trạng thái tạm thời.
- `ShowWarning(...)`: thao tác chưa thể tiếp tục do state hiện tại, dữ liệu thiếu hoặc tính năng chưa sẵn sàng.
- `ShowError(...)`: thao tác thất bại hoặc không thể hoàn tất.
- Nội dung toast phải là tiếng Việt có dấu, ngắn gọn, đúng ngữ cảnh và không chứa dữ liệu nhạy cảm.

## 5. Mẫu dùng chuẩn

```razor
@inject IHrmToastService ToastService
```

```csharp
ToastService.ShowSuccess("Đã lưu nhân viên.");
ToastService.ShowWarning("Hãy chọn ít nhất một dòng.");
```

Khi cần cấu hình nâng cao theo API DevExpress, vẫn đi qua shared service:

```razor
ToastService.Show(
    "Tiến trình đang chạy: kiểm tra dữ liệu.",
    configure: options => {
        options.DisplayTime = TimeSpan.Zero;
        options.RenderStyle = ToastRenderStyle.Primary;
        options.ThemeMode = ToastThemeMode.Pastel;
    },
    template: @<div class="d-flex gap-2">
        <DxButton RenderStyle="ButtonRenderStyle.Warning">Xem chi tiết</DxButton>
        <DxButton RenderStyle="ButtonRenderStyle.Secondary">Đóng</DxButton>
    </div>);
```

## 6. Quy tắc rà soát bắt buộc

Trước khi đóng màn hình hoặc sprint có chạm UI, phải rà ít nhất một lượt:

```powershell
rg -n -F -e 'IToastNotificationService' -e 'ShowToast(' -e 'CloseToast(' -e 'DxToastProvider' -e '<DxToast' -e 'ToastOptions' -e 'ProviderName' src\Vnta.HRM2026
```

Kết quả hợp lệ chỉ nên còn trong shared layer:

- `Services/Ui/IHrmToastService.cs`
- `Services/Ui/HrmToastService.cs`
- `Services/Ui/HrmToastDefaults.cs`
- `Components/Shared/Feedback/HrmToastProvider.razor`

Nếu grep còn trả về business page hoặc component UI khác, phải refactor về `IHrmToastService` trước khi merge.

## 7. Tài liệu tham chiếu

- DevExpress Blazor Toast: `https://docs.devexpress.com/Blazor/405068/components/dialogs-and-windows/toast`
- Theme variables example: `https://github.com/DevExpress-Examples/blazor-use-devexpress-theme-variables`
