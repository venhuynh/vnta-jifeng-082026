using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Đại diện kiểu <c>PhuCapComLockActionPopup</c> phục vụ màn hình phụ cấp cơm.</summary>
public partial class PhuCapComLockActionPopup
{
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

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => IsRefreshing ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);
    /// <summary>Cập nhật lựa chọn cho luồng <c>SelectScopeAsync</c>.</summary>
    private Task SelectScopeAsync(string scope) => IsRefreshing ? Task.CompletedTask : ScopeSelected.InvokeAsync(scope);
    /// <summary>Xác nhận cho luồng <c>ConfirmAsync</c>.</summary>
    private Task ConfirmAsync() => CanConfirm ? ConfirmRequested.InvokeAsync() : Task.CompletedTask;

    /// <summary>Lấy cho luồng <c>GetScopeCssClass</c>.</summary>
    private string GetScopeCssClass(string scope)
    {
        var cssClasses = new List<string> { "meal-allowance-lock-action-option" };
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
