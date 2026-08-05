using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

public partial class PhuCapTongHopRefreshConfirmPopup
{
    private static readonly Dictionary<string, object> HeaderIconAttributes =
        new Dictionary<string, object>
        {
            ["aria-hidden"] = "true",
            ["tabindex"] = "-1"
        };

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public string PeriodLabel { get; set; } = string.Empty;
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) =>
        !visible && IsRefreshing ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() =>
        IsRefreshing ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    private Task ConfirmAsync() =>
        IsRefreshing ? Task.CompletedTask : ConfirmRequested.InvokeAsync();
}
