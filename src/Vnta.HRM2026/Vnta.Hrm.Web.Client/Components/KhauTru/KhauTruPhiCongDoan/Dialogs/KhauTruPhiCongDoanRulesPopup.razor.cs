using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruPhiCongDoan;

public partial class KhauTruPhiCongDoanRulesPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public string PeriodLabel { get; set; } = string.Empty;

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
}
