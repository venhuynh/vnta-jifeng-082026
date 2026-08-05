using Microsoft.AspNetCore.Components;
using System.Globalization;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Dialogs;

/// <summary>Đại diện kiểu <c>PhuCapChuyenCanRulesPopup</c> phục vụ màn hình phụ cấp chuyên cần.</summary>
public partial class PhuCapChuyenCanRulesPopup
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public AttendanceAllowanceRuleDto? Rule { get; set; }
    [Parameter] public bool IsRuleLoading { get; set; }

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);

    /// <summary>Đóng cho luồng <c>CloseAsync</c>.</summary>
    private Task CloseAsync() => VisibleChanged.InvokeAsync(false);

    private static string FormatDecimal(decimal value, int decimalPlaces) =>
        value.ToString($"N{Math.Max(decimalPlaces, 0)}", DisplayCulture);

    private static string FormatMoney(decimal value) => $"{value.ToString("N0", DisplayCulture)} đồng";
}
