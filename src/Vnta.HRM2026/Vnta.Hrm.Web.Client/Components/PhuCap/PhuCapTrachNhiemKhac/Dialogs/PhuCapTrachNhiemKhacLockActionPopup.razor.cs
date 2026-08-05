using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Dialogs;

public partial class PhuCapTrachNhiemKhacLockActionPopup
{
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter, EditorRequired]
    public global::Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.OtherResponsibilityAllowanceLockActionDialogState State { get; set; } = default!;
    [Parameter] public EventCallback<string> ScopeSelected { get; set; }
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    private bool Visible => State.Visible;
    private bool IsBusy => State.IsBusy;
    private bool ShouldLock => State.ShouldLock;
    private bool CanConfirm => State.CanConfirm;
    private bool CanChooseSelectedRowsScope => State.CanChooseSelectedRowsScope;
    private string Title => State.Title;
    private string PromptText => State.PromptText;
    private string ContextText => State.ContextText;
    private string SelectedScope => State.SelectedScope;
    private string SelectedRowsScope => State.SelectedRowsScope;
    private string WholePeriodScope => State.WholePeriodScope;
    private string WholePeriodLabel => State.WholePeriodLabel;
    private string SelectedRowsDescription => State.SelectedRowsDescription;
    private string WholePeriodDescription => State.WholePeriodDescription;

    private Task OnVisibleChangedAsync(bool visible) =>
        IsBusy ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    private Task CloseAsync() =>
        IsBusy ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    private Task SelectScopeAsync(string scope) =>
        IsBusy ? Task.CompletedTask : ScopeSelected.InvokeAsync(scope);

    private Task ConfirmAsync() =>
        IsBusy ? Task.CompletedTask : ConfirmRequested.InvokeAsync();

    private string GetScopeCssClass(string scope)
    {
        var cssClasses = new List<string> { "responsibility-lock-action-option" };
        if(string.Equals(SelectedScope, scope, StringComparison.Ordinal))
        {
            cssClasses.Add("is-active");
        }

        if(string.Equals(scope, SelectedRowsScope, StringComparison.Ordinal)
            && !CanChooseSelectedRowsScope)
        {
            cssClasses.Add("is-disabled");
        }

        return string.Join(' ', cssClasses);
    }
}
