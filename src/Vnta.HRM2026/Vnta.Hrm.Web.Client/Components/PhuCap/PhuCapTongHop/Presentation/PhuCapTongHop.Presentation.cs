using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Pure presentation rules and display helpers for the route host.</summary>
public partial class PhuCapTongHop
{
    private IReadOnlyList<MonthOption> AvailableToolbarMonthOptions =>
        ToolbarYear == MinimumSupportedYear
            ? AvailableMonthOptions.Where(option => option.Value >= MinimumSupportedMonth).ToArray()
            : AvailableMonthOptions;

    private IReadOnlyList<AllowanceAmountSummary> VisibleAllowanceSummaries =>
    new AllowanceAmountSummary[]
    {
        new("Tổng phụ cấp", VisibleAllowanceTotals.Total, "attendance-allowance-total-amount-button"),
        new("Trách nhiệm", VisibleAllowanceTotals.Responsibility, "attendance-allowance-total-amount-button"),
        new("Trách nhiệm khác", VisibleAllowanceTotals.ResponsibilityOther, "attendance-allowance-total-amount-button"),
        new("Thâm niên", VisibleAllowanceTotals.Seniority, "attendance-allowance-total-amount-button"),
        new("Chuyên cần", VisibleAllowanceTotals.Attendance, "attendance-allowance-total-amount-button"),
        new("Cơm", VisibleAllowanceTotals.Meal, "attendance-allowance-total-amount-button"),
        new("Độc hại", VisibleAllowanceTotals.Hazard, "attendance-allowance-total-amount-button"),
        new("Khác", VisibleAllowanceTotals.Other, "attendance-allowance-total-amount-button"),
        new("Phép-Lễ", VisibleAllowanceTotals.LeaveHoliday, "attendance-allowance-total-amount-button")
    }.Where(summary => summary.Amount != 0m).ToArray();

    private string EmptyStateTitle => !HasRequestedData
        ? "Chưa tải dữ liệu tổng hợp phụ cấp"
        : !string.IsNullOrWhiteSpace(SearchText)
            ? "Không tìm thấy kết quả tổng hợp phụ cấp phù hợp"
            : ActiveSummaryBadgeKey == SummaryAllKey ? "Chưa có dữ liệu tổng hợp phụ cấp" : "Không có dữ liệu ở trạng thái đã chọn";

    private string EmptyStateMessage => !HasRequestedData
        ? "Chọn tháng, năm kỳ lương rồi nhấn Xem để tải dữ liệu khi bạn sẵn sàng."
        : !string.IsNullOrWhiteSpace(SearchText)
            ? "Hãy thử từ khóa khác hoặc xóa tìm kiếm để xem thêm dữ liệu."
            : ActiveSummaryBadgeKey == SummaryAllKey
                ? "Bảng tổng hợp phụ cấp sẽ hiển thị tại đây sau khi có dữ liệu cho kỳ lương đang chọn."
                : "Hãy nới điều kiện lọc hoặc chuyển sang nhóm trạng thái khác để xem thêm dữ liệu.";

    private string EmptyStateActionText => !HasRequestedData ? "Xem dữ liệu" : "Tải lại";
    private string PageSizeDescription => "dòng/trang";
    private string SyncSourcePeriodDisplay
    {
        get
        {
            var sourcePeriod = GetPreviousPeriod(AppliedMonth, AppliedYear);
            return $"{sourcePeriod.Month:00}/{sourcePeriod.Year}";
        }
    }
    private string SyncTargetPeriodDisplay => $"{AppliedMonth:00}/{AppliedYear}";
    private string LockActionPopupTitle => PendingLockActionState ? "Khóa dữ liệu phụ cấp tổng hợp" : "Mở khóa dữ liệu phụ cấp tổng hợp";
    private string LockActionPromptText => PendingLockActionState
        ? "Chọn phạm vi cần khóa dữ liệu phụ cấp tổng hợp. Các dòng đã khóa sẽ không thể nhập tay, xóa hoặc làm mới phụ cấp."
        : "Chọn phạm vi cần mở khóa dữ liệu phụ cấp tổng hợp.";
    private string LockActionScopeContextText => $"Kỳ lương áp dụng: {PendingLockActionPeriodDisplay}. Lựa chọn toàn bộ kỳ không phụ thuộc bộ lọc, phân trang hoặc kết quả tìm kiếm đang hiển thị.";
    private string SelectedRowsScopeDescription => CanChooseSelectedRowsScope
        ? $"Áp dụng cho {SelectedRowCount:N0} dòng đang được chọn trong trang dữ liệu hiện tại."
        : "Chưa có dòng nào được chọn trong dữ liệu hiện tại.";
    private string WholePeriodScopeDescription => PendingLockActionState
        ? $"Khóa toàn bộ dữ liệu tổng hợp phụ cấp của kỳ {PendingLockActionPeriodDisplay}."
        : $"Mở khóa toàn bộ dữ liệu tổng hợp phụ cấp của kỳ {PendingLockActionPeriodDisplay}.";

    private bool CanEditRow(PayrollAllowanceSummaryRecord row) => CanOperateOnCurrentDataset && !row.IsLocked;
    private bool CanRefreshRow(PayrollAllowanceSummaryRecord row) => CanOperateOnCurrentDataset && !row.IsLocked;
    private bool CanToggleLock(PayrollAllowanceSummaryRecord _) => CanOperateOnCurrentDataset;
    private static string GetLockActionText(PayrollAllowanceSummaryRecord row) => row.IsLocked ? "Mở khóa" : "Khóa";
    private bool? GetLockFilterValue() => ActiveSummaryBadgeKey switch { SummaryOpenKey => false, SummaryLockedKey => true, _ => null };
    private string GetLockBadgeCssClass(bool isLocked) => isLocked ? "yes-no-status yes-no-status-no hrm-grid-status" : "yes-no-status yes-no-status-yes hrm-grid-status";
    private static bool IsWholePeriodLockActionScope(string scope) => string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);

    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = FormatOptional(value);
        if(string.IsNullOrWhiteSpace(SearchText) || SearchText.Trim().Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);
        while(true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if(matchIndex < 0)
            {
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"allowance-summary-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if(builder.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    private string FormatMoney(decimal value) => value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);
    private string FormatSummaryMoney(decimal value) => FormatMoney(value);
    private static string FormatOptional(string? value) => string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
    private static IReadOnlyList<AllowanceSummaryBadge> BuildSummaryBadges(PayrollAllowanceSummaryOverviewDto summary) =>
    [new(SummaryAllKey, "Tất cả", summary.TotalCount), new(SummaryOpenKey, "Đang mở", summary.OpenCount), new(SummaryLockedKey, "Đã khóa", summary.LockedCount)];

    private async Task RenderBusyStateAsync()
    {
        await InvokeAsync(StateHasChanged);
        await Task.Yield();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? ValidateManualEditModel(PhuCapTongHopManualEditModel model)
    {
        var allowanceAmounts = new (string Label, decimal Amount)[]
        {
            ("Phụ cấp trách nhiệm", model.ResponsibilityAllowanceAmount),
            ("Phụ cấp thâm niên", model.SeniorityAllowanceAmount),
            ("Phụ cấp chuyên cần", model.AttendanceAllowanceAmount),
            ("Phụ cấp cơm", model.MealAllowanceAmount),
            ("Phụ cấp độc hại", model.HazardAllowanceAmount),
            ("Phụ cấp khác", model.OtherAllowanceAmount),
            ("Phụ cấp phép/lễ", model.LeaveHolidayAllowanceAmount)
        };

        var invalidAllowance = allowanceAmounts.FirstOrDefault(item => item.Amount < 0);
        if (invalidAllowance.Amount < 0)
        {
            return $"{invalidAllowance.Label} không được nhỏ hơn 0.";
        }

        if (!string.IsNullOrWhiteSpace(model.Note) && model.Note.Trim().Length > 1000)
        {
            return "Ghi chú không được vượt quá 1000 ký tự.";
        }

        model.Note = NormalizeOptional(model.Note);
        return null;
    }

    private static (int Month, int Year) GetPreviousPeriod(int month, int year) =>
        month == 1 ? (12, year - 1) : (month - 1, year);

    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        return normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth
            ? (MinimumSupportedMonth, MinimumSupportedYear)
            : (normalizedMonth, normalizedYear);
    }

    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }

    private static IReadOnlyList<MonthOption> BuildMonthOptions() =>
        Enumerable.Range(1, 12).Select(month => new MonthOption(month, $"Tháng {month:00}")).ToArray();
}
