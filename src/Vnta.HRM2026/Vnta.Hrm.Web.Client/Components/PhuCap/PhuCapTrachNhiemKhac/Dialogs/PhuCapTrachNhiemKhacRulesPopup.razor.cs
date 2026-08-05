using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Dialogs;

public partial class PhuCapTrachNhiemKhacRulesPopup
{
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }

    [Parameter, EditorRequired]
    public global::Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.OtherResponsibilityAllowanceRulesDialogState State { get; set; } = default!;

    private bool Visible => State.Visible;

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
}
