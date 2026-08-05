using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Trình bày ngữ cảnh điều chỉnh do backend cung cấp và phát yêu cầu lưu về cha.
/// </summary>
public partial class PhuCapTrachNhiemAdjustmentPopup
{
    [Parameter] public bool IsAdjustmentPopupVisible { get; set; }
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }
    [Parameter] public string AdjustmentPopupTitle { get; set; } = string.Empty;
    [Parameter] public string? AdjustmentPopupErrorMessage { get; set; }
    [Parameter] public bool IsLoadingAdjustmentContext { get; set; }
    [Parameter] public PayrollResponsibilityAllowanceUpdateContextDto? AdjustmentContext { get; set; }
    [Parameter] public AdjustmentFormModel AdjustmentForm { get; set; } = AdjustmentFormModel.CreateDefault();
    [Parameter] public IReadOnlyList<PayrollResponsibilityAllowanceGradeDto> GradeRows { get; set; } = [];
    [Parameter] public EventCallback<decimal> PerformanceBonusChanged { get; set; }
    [Parameter] public EventCallback SaveRequested { get; set; }

    /// <summary>Xử lý sự kiện cho luồng <c>OnVisibleChangedAsync</c>.</summary>
    private Task OnVisibleChangedAsync(bool visible) => VisibleChanged.InvokeAsync(visible);
    /// <summary>Xử lý sự kiện cho luồng <c>OnAdjustmentPerformanceBonusChanged</c>.</summary>
    private Task OnAdjustmentPerformanceBonusChanged(decimal value) => PerformanceBonusChanged.InvokeAsync(value);
    /// <summary>Lưu cho luồng <c>SaveAdjustmentAsync</c>.</summary>
    private Task SaveAdjustmentAsync() => SaveRequested.InvokeAsync();

    /// <summary>Định dạng cho luồng <c>FormatCurrency</c>.</summary>
    private static string FormatCurrency(decimal value) =>
        value == 0m ? string.Empty : string.Format(CultureInfo.GetCultureInfo("vi-VN"), "{0:N0} đ", value);
    /// <summary>Định dạng cho luồng <c>FormatNumber</c>.</summary>
    private static string FormatNumber(decimal value) => value.ToString("0.##");
}
