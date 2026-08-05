using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruPhiCongDoan;

public partial class KhauTruPhiCongDoanEditPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public string Title { get; set; } = "Điều chỉnh phí công đoàn";
    [Parameter] public KhauTruPhiCongDoanEditModel Model { get; set; } = new();
    [Parameter] public bool CanEditFields { get; set; }
    [Parameter] public bool CanSave { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    private Task SaveAsync() => SaveRequested.InvokeAsync();

    private Task OnDeductionAmountChangedAsync(decimal value)
    {
        Model.DeductionAmount = Math.Max(0m, value);
        return Task.CompletedTask;
    }
}
