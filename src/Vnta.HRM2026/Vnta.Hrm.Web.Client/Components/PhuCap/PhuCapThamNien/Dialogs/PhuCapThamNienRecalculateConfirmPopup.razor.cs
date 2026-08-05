using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Đại diện kiểu <c>PhuCapThamNienRecalculateConfirmPopup</c> phục vụ giao diện phụ cấp thâm niên.</summary>
public partial class PhuCapThamNienRecalculateConfirmPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public bool IsRefreshing { get; set; }
    [Parameter] public string PeriodLabel { get; set; } = string.Empty;
    [Parameter] public EventCallback ConfirmRequested { get; set; }

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
    /// <summary>Xác nhận cho luồng <c>ConfirmAsync</c>.</summary>
    private Task ConfirmAsync() => ConfirmRequested.InvokeAsync();
}
