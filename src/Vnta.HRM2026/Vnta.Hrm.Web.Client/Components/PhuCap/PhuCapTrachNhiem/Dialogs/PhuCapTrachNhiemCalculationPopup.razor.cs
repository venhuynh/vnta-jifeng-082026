using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Đại diện kiểu <c>PhuCapTrachNhiemCalculationPopup</c> phục vụ màn hình phụ cấp trách nhiệm.</summary>
public partial class PhuCapTrachNhiemCalculationPopup
{
    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public string Title { get; set; } = string.Empty;
    [Parameter] public PayrollResponsibilityAllowanceAbcItemDto? Record { get; set; }
    [Parameter] public string Description { get; set; } = string.Empty;
    [Parameter] public string Formula { get; set; } = string.Empty;
    [Parameter] public IReadOnlyList<CalculationDetailRow> Details { get; set; } = [];

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    /// <summary>Định dạng cho luồng <c>FormatCurrency</c>.</summary>
    private static string FormatCurrency(decimal value) =>
        value == 0m ? string.Empty : string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} đ", value);
}
