using DevExpress.Blazor;

namespace Vnta.Hrm.Web.Client.Services.Ui;

public interface IHrmDialogService {
    Task AlertAsync(
        string message,
        string? title = null,
        string okText = "Đóng",
        MessageBoxRenderStyle renderStyle = MessageBoxRenderStyle.Primary);

    Task<bool> ConfirmAsync(
        string message,
        string? title = null,
        string okText = "Đồng ý",
        string cancelText = "Hủy",
        MessageBoxRenderStyle renderStyle = MessageBoxRenderStyle.Primary);
}
