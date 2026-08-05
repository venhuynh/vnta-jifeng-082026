using System.Globalization;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Sections;

public partial class PhuCapDocHaiGrid
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");
    private IGrid? grid;

    public IGrid? Grid => grid;

    [Parameter] public IReadOnlyList<HazardAllowanceListItemDto> Rows { get; set; } = [];
    [Parameter] public IReadOnlyList<object> SelectedItems { get; set; } = [];
    [Parameter] public bool CanOperateOnCurrentDataset { get; set; }
    [Parameter] public int PageIndex { get; set; }
    [Parameter] public int PageSize { get; set; }
    [Parameter] public string EmptyStateTitle { get; set; } = string.Empty;
    [Parameter] public string EmptyStateMessage { get; set; } = string.Empty;
    [Parameter] public string EmptyStateActionText { get; set; } = string.Empty;
    [Parameter] public Func<HazardAllowanceListItemDto, bool> CanEditRow { get; set; } = _ => false;
    [Parameter] public Func<HazardAllowanceListItemDto, bool> CanRefreshRow { get; set; } = _ => false;
    [Parameter] public Func<HazardAllowanceListItemDto, bool> CanToggleLock { get; set; } = _ => false;
    [Parameter] public Func<HazardAllowanceListItemDto, bool> CanViewMonthlyWork { get; set; } = _ => false;
    [Parameter] public Func<string?, MarkupString> HighlightSearchText { get; set; } = _ => new MarkupString(string.Empty);
    [Parameter] public EventCallback<IReadOnlyList<object>> SelectedItemsChanged { get; set; }
    [Parameter] public EventCallback<GridFilterCriteriaChangedEventArgs> FilterCriteriaChanged { get; set; }
    [Parameter] public EventCallback EmptyStateActionRequested { get; set; }
    [Parameter] public EventCallback<HazardAllowanceListItemDto> EditRequested { get; set; }
    [Parameter] public EventCallback<HazardAllowanceListItemDto> RefreshRequested { get; set; }
    [Parameter] public EventCallback<HazardAllowanceListItemDto> LockToggleRequested { get; set; }
    [Parameter] public EventCallback<HazardAllowanceListItemDto> MonthlyWorkRequested { get; set; }

    private static string FormatEmployeeDisplay(HazardAllowanceListItemDto row) =>
        string.Join(" - ", new[] { row.EmployeeCode, row.EmployeeName }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    private static string FormatPreciseWorkday(decimal value) => decimal.Round(value, 3, MidpointRounding.AwayFromZero).ToString("0.000", DisplayCulture);
    private static string FormatMoney(decimal value)
    {
        var rounded = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
        return rounded == 0m ? string.Empty : $"{rounded.ToString("#,##0", DisplayCulture)} đ";
    }

    private static string GetStatusBadgeCssClass(bool isEligible) => isEligible
        ? "yes-no-status yes-no-status-yes hrm-grid-status"
        : "yes-no-status yes-no-status-no hrm-grid-status";

    private static string GetLockBadgeCssClass(bool isLocked) => isLocked
        ? "yes-no-status yes-no-status-no hrm-grid-status"
        : "yes-no-status yes-no-status-yes hrm-grid-status";

    private static string GetLockActionText(HazardAllowanceListItemDto row) => row.IsLocked ? "Mở khóa" : "Khóa";
    private static string GetLockActionIcon(HazardAllowanceListItemDto row) => row.IsLocked ? VntaDevExpressIcons.Unlock : VntaDevExpressIcons.Lock;
    private static string GetLockActionTooltip(HazardAllowanceListItemDto row) => row.IsLocked ? "Mở khóa dòng phụ cấp độc hại này." : "Khóa dòng phụ cấp độc hại này để chặn điều chỉnh và làm mới.";
}
