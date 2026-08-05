using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Dialogs;

public partial class PhuCapTrachNhiemKhacRecalculateConfirmPopup
{
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter, EditorRequired]
    public global::Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.OtherResponsibilityAllowanceRecalculateDialogState State { get; set; } = default!;
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    private bool Visible => State.Visible;
    private bool IsBusy => State.IsBusy;
    private bool CanConfirm => State.CanConfirm;
    private string PeriodLabel => State.PeriodLabel;

    private Task OnVisibleChangedAsync(bool visible) =>
        IsBusy ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() =>
        IsBusy ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    private Task ConfirmAsync() =>
        IsBusy || !CanConfirm ? Task.CompletedTask : ConfirmRequested.InvokeAsync();
}
