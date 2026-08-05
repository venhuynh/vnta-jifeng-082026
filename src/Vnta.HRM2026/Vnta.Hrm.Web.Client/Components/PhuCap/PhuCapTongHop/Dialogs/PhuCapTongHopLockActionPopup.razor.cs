using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

public partial class PhuCapTongHopLockActionPopup
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
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public string PromptText { get; set; } = string.Empty;
    [Parameter] public string ContextText { get; set; } = string.Empty;
    [Parameter] public string SelectedScope { get; set; } = string.Empty;
    [Parameter] public string SelectedRowsScope { get; set; } = string.Empty;
    [Parameter] public string WholePeriodScope { get; set; } = string.Empty;
    [Parameter] public string SelectedRowsDescription { get; set; } = string.Empty;
    [Parameter] public string WholePeriodDescription { get; set; } = string.Empty;
    [Parameter] public string WholePeriodLabel { get; set; } = string.Empty;
    [Parameter] public bool CanChooseSelectedRowsScope { get; set; }
    [Parameter] public bool CanConfirm { get; set; }
    [Parameter] public bool ShouldLock { get; set; }
    [Parameter] public EventCallback<string> ScopeSelected { get; set; }
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    private Task OnVisibleChangedAsync(bool visible) =>
        !visible && IsRefreshing ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() =>
        IsRefreshing ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    private Task SelectScopeAsync(string scope) =>
        IsRefreshing ? Task.CompletedTask : ScopeSelected.InvokeAsync(scope);

    private Task ConfirmAsync() =>
        IsRefreshing || !CanConfirm ? Task.CompletedTask : ConfirmRequested.InvokeAsync();

    private string GetScopeCssClass(string scope)
    {
        var cssClasses = new List<string> { "allowance-summary-lock-action-option" };
        if(string.Equals(SelectedScope, scope, StringComparison.Ordinal))
        {
            cssClasses.Add("is-active");
        }

        if(string.Equals(scope, SelectedRowsScope, StringComparison.Ordinal) && !CanChooseSelectedRowsScope)
        {
            cssClasses.Add("is-disabled");
        }

        return string.Join(' ', cssClasses);
    }
}
