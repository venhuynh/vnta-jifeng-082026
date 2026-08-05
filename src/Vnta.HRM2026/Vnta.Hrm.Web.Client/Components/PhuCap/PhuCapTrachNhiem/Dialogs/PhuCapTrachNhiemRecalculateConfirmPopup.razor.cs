using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiemRecalculateConfirmPopup</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public partial class PhuCapTrachNhiemRecalculateConfirmPopup
{
    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp trách nhiệm.</summary>
    private static readonly Dictionary<string, object> DecorativeIconButtonAttributes = new()
    {
        ["aria-hidden"] = "true",
        ["tabindex"] = "-1"
    };

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
