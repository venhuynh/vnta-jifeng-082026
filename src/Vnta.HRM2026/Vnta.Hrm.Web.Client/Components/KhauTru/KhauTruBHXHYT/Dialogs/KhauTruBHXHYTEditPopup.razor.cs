using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT;

public partial class KhauTruBHXHYTEditPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Điều chỉnh khấu trừ BHXH-YT";
    [Parameter] public KhauTruBHXHYTEditModel? Model { get; set; }
    [Parameter] public EditContext? EditContext { get; set; }
    [Parameter] public string? ErrorMessage { get; set; }
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);

    private async Task SaveAsync()
    {
        if (EditContext is null || !EditContext.Validate())
        {
            return;
        }

        await SaveRequested.InvokeAsync();
    }

    private static string FormatMoney(decimal value) => value.ToString("N2", DisplayCulture);
    private static string FormatRate(decimal value) => value.ToString("P2", DisplayCulture);
}
