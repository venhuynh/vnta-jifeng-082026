using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Dialogs;

/// <summary>Đại diện kiểu <c>PhuCapChuyenCanRecalculateConfirmPopup</c> phục vụ màn hình phụ cấp chuyên cần.</summary>
public partial class PhuCapChuyenCanRecalculateConfirmPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public string PeriodLabel { get; set; } = string.Empty;
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) =>
        !visible && IsRefreshing ? Task.CompletedTask : VisibleChanged.InvokeAsync(visible);

    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => IsRefreshing ? Task.CompletedTask : VisibleChanged.InvokeAsync(false);

    /// <summary>Xác nhận cho luồng <c>ConfirmAsync</c>.</summary>
    private Task ConfirmAsync() => IsRefreshing ? Task.CompletedTask : ConfirmRequested.InvokeAsync();
}
