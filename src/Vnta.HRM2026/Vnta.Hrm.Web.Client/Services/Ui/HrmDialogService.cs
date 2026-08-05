using DevExpress.Blazor;

namespace Vnta.Hrm.Web.Client.Services.Ui;

public sealed class HrmDialogService(IDialogService dialogService) : IHrmDialogService {
    public Task AlertAsync(
        string message,
        string? title = null,
        string okText = "Đóng",
        MessageBoxRenderStyle renderStyle = MessageBoxRenderStyle.Primary) =>
        dialogService.AlertAsync(new MessageBoxOptions {
            Title = title ?? "Thông báo",
            Text = message,
            OkButtonText = okText,
            RenderStyle = renderStyle,
            ShowCloseButton = true
        });

    public Task<bool> ConfirmAsync(
        string message,
        string? title = null,
        string okText = "Đồng ý",
        string cancelText = "Hủy",
        MessageBoxRenderStyle renderStyle = MessageBoxRenderStyle.Primary) =>
        dialogService.ConfirmAsync(new MessageBoxOptions {
            Title = title ?? "Xác nhận",
            Text = message,
            OkButtonText = okText,
            CancelButtonText = cancelText,
            RenderStyle = renderStyle,
            ShowCloseButton = true
        });
}
