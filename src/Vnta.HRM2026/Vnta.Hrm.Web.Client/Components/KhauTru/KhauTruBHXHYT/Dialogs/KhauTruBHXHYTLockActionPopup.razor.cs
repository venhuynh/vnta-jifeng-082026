using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruBHXHYT;

public partial class KhauTruBHXHYTLockActionPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsBusy { get; set; }
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

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    private Task SelectScopeAsync(string scope) => ScopeSelected.InvokeAsync(scope);
    private Task ConfirmAsync() => ConfirmRequested.InvokeAsync();

    private string GetScopeCssClass(string scope)
    {
        var cssClasses = new List<string> { "insurance-deduction-lock-action-option" };
        if (string.Equals(SelectedScope, scope, StringComparison.Ordinal))
        {
            cssClasses.Add("is-active");
        }

        if (string.Equals(scope, SelectedRowsScope, StringComparison.Ordinal)
            && !CanChooseSelectedRowsScope)
        {
            cssClasses.Add("is-disabled");
        }

        return string.Join(' ', cssClasses);
    }
}
