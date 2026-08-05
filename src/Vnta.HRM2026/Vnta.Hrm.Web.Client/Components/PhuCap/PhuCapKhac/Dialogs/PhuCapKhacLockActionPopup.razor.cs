using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.State;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

/// <summary>Cho phép chọn phạm vi trước khi khóa hoặc mở khóa phụ cấp khác.</summary>
public partial class PhuCapKhacLockActionPopup
{
    [Parameter, EditorRequired] public OtherAllowanceLockActionDialogState State { get; set; } = default!;
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public EventCallback<string> ScopeSelected { get; set; }
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    private string GetScopeCssClass(string scope)
    {
        var cssClasses = new List<string> { "other-allowance-lock-action-option" };
        if(string.Equals(State.SelectedScope, scope, StringComparison.Ordinal)) cssClasses.Add("is-active");
        if(string.Equals(scope, State.SelectedRowsScope, StringComparison.Ordinal) && !State.CanChooseSelectedRowsScope)
            cssClasses.Add("is-disabled");
        return string.Join(' ', cssClasses);
    }

    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    private Task CloseAsync() => State.IsRefreshing ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);
    private Task SelectScopeAsync(string scope) => State.IsRefreshing ? Task.CompletedTask : ScopeSelected.InvokeAsync(scope);
    private Task ConfirmAsync() => State.CanConfirm ? ConfirmRequested.InvokeAsync() : Task.CompletedTask;
}
