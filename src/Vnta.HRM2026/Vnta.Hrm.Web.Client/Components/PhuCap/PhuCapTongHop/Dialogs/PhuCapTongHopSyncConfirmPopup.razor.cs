using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

public partial class PhuCapTongHopSyncConfirmPopup
{
    private static readonly Dictionary<string, object> HeaderIconAttributes =
        new()
        {
            ["aria-hidden"] = "true",
            ["tabindex"] = "-1"
        };

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsSyncing { get; set; }
    [Parameter] public string SourcePeriodLabel { get; set; } = string.Empty;
    [Parameter] public string TargetPeriodLabel { get; set; } = string.Empty;
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) =>
        !visible && IsSyncing ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() =>
        IsSyncing ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    private Task ConfirmAsync() =>
        IsSyncing ? Task.CompletedTask : ConfirmRequested.InvokeAsync();
}
