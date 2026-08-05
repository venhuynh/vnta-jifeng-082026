using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

public partial class PhuCapTrachNhiemRulesPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }

    private Task OnVisibleChangedAsync(bool value) => VisibleChanged.InvokeAsync(value);

    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
}
