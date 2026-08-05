using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHaiRulesPopup</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHaiRulesPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public string PayrollPeriod { get; set; } = string.Empty;
    [Parameter] public int LoadedRecordCount { get; set; }
    [Parameter] public int VisibleRecordCount { get; set; }
    [Parameter] public bool HasRequestedData { get; set; }

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);

    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);
}
