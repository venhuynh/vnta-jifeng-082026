using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Owns screen messages, display formatting and derived snapshot defaults.</summary>
public partial class PhuCapCom
{
    /// <summary>Tạo cho luồng <c>BuildLockActionNoDataMessage</c>.</summary>
    private string BuildLockActionNoDataMessage(bool shouldLock, string scope) =>
        IsWholePeriodLockActionScope(scope)
            ? $"Không có dữ liệu phụ cấp cơm của kỳ {PendingLockActionPeriodLabel} để {(shouldLock ? "khóa" : "mở khóa")}."
            : "Không còn dòng phụ cấp cơm hợp lệ trong phạm vi đang chọn để xử lý.";

    /// <summary>Tạo cho luồng <c>BuildLockActionAlreadyAppliedMessage</c>.</summary>
    private string BuildLockActionAlreadyAppliedMessage(bool shouldLock, string scope, int targetRowCount)
    {
        var stateText = shouldLock ? "khóa" : "mở";
        return IsWholePeriodLockActionScope(scope)
            ? $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {targetRowCount:N0} dòng của kỳ {PendingLockActionPeriodLabel} đã ở trạng thái {stateText}."
            : $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {targetRowCount:N0} dòng đã chọn đã ở trạng thái {stateText}.";
    }

    /// <summary>Tạo cho luồng <c>BuildLockActionPendingLoadingMessage</c>.</summary>
    private string BuildLockActionPendingLoadingMessage(bool shouldLock, string scope, int selectedRowCount)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        return IsWholePeriodLockActionScope(scope)
            ? $"Đang xử lý {actionText} dữ liệu phụ cấp cơm của kỳ {PendingLockActionPeriodLabel}..."
            : selectedRowCount > 0
                ? $"Đang xử lý {actionText} {selectedRowCount:N0} dòng phụ cấp cơm đã chọn..."
                : $"Đang xử lý {actionText} các dòng phụ cấp cơm đã chọn...";
    }

    /// <summary>Tạo cho luồng <c>BuildLockActionSuccessMessage</c>.</summary>
    private string BuildLockActionSuccessMessage(bool shouldLock, string scope, int targetRowCount, int updatedCount)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var unchangedCount = Math.Max(0, targetRowCount - updatedCount);
        if(IsWholePeriodLockActionScope(scope))
        {
            return unchangedCount > 0
                ? $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} dòng phụ cấp cơm của kỳ {PendingLockActionPeriodLabel}, giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái."
                : $"Đã {actionText} {updatedCount:N0} dòng phụ cấp cơm của kỳ {PendingLockActionPeriodLabel}.";
        }

        return unchangedCount > 0
            ? $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} dòng đã chọn, giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái."
            : $"Đã {actionText} {updatedCount:N0} dòng phụ cấp cơm đã chọn.";
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ShowRefreshResultToast</c>.</summary>
    private void ShowRefreshResultToast(RefreshMealAllowanceResult result)
    {
        var targetPayrollPeriod = FormatPayrollPeriod(result.TargetPayrollMonth, result.TargetPayrollYear);

        if(result.SummaryTargetCount == 0)
        {
            ToastService.ShowInfo(
                $"Chưa có dòng tổng hợp phụ cấp nào cho kỳ {targetPayrollPeriod} để tạo dữ liệu phụ cấp cơm.");
            return;
        }

        ToastService.ShowSuccess(
            $"Đã tính lại phụ cấp cơm kỳ {targetPayrollPeriod}: đồng bộ {result.SummaryTargetCount:N0} dòng tổng hợp, đủ điều kiện {result.QualifiedEmployeeCount:N0}, tạo mới {result.CreatedCount:N0}, cập nhật {result.UpdatedCount:N0}, bỏ qua khóa {result.SkippedLockedCount:N0}, giữ điều chỉnh {result.SkippedManualAdjustmentCount:N0}.");
    }

    /// <summary>Thực hiện xử lý cho luồng <c>InitializeNewResultDefaults</c>.</summary>
    private void InitializeNewResultDefaults(MealAllowanceRecord model)
    {
        var (payrollMonth, payrollYear) = GetAppliedPayrollPeriod();

        model.Id = Guid.NewGuid();
        model.EmployeeId = null;
        model.EmployeeCode = null;
        model.EmployeeName = null;
        model.DepartmentName = null;
        model.PositionName = null;
        model.PayrollMonth = payrollMonth;
        model.PayrollYear = payrollYear;
        model.QualifiedMealDays = 0;
        model.Overtime1900Days = 0;
        model.MealAllowancePerQualifiedDay = MealAllowancePolicy.DefaultMealAllowancePerQualifiedDay;
        model.RuleCode = MealAllowancePolicy.QualifiedMealRuleCode;
        model.RuleVersion = MealAllowancePolicy.QualifiedMealRuleVersion;
        model.Note = null;
        model.IsLocked = false;
        model.CalculatedAtUtc = DateTime.UtcNow;
        model.CreatedAtUtc = DateTime.UtcNow;
        model.UpdatedAtUtc = null;
        model.RecalculateDerivedValues();
    }

    /// <summary>Thực hiện xử lý cho luồng <c>HighlightSearchText</c>.</summary>
    private MarkupString HighlightSearchText(string? value, string fallback = "Chưa có")
    {
        var displayText = FormatOptional(value, fallback);
        if(string.IsNullOrWhiteSpace(SearchText))
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        if(searchText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

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
            builder.Append("<mark class=\"meal-search-highlight\">");
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

    /// <summary>Định dạng cho luồng <c>FormatMoney</c>.</summary>
    private string FormatMoney(decimal value) =>
        value == 0m ? string.Empty : $"{value.ToString("N0", DisplayCulture)} đ";

    /// <summary>Định dạng cho luồng <c>FormatNonZeroMoney</c>.</summary>
    private string FormatNonZeroMoney(decimal value) => FormatMoney(value);

    /// <summary>Định dạng cho luồng <c>FormatNonZeroValue</c>.</summary>
    private string FormatNonZeroValue(int value) =>
        value == 0 ? string.Empty : value.ToString("N0", DisplayCulture);

    /// <summary>Định dạng cho luồng <c>FormatOptional</c>.</summary>
    private static string FormatOptional(string? value, string fallback = "Chưa có") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeNullable</c>.</summary>
    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Định dạng cho luồng <c>FormatPayrollPeriod</c>.</summary>
    private static string FormatPayrollPeriod(int payrollMonth, int payrollYear) =>
        $"{payrollMonth:00}/{payrollYear}";

    /// <summary>Tạo cho luồng <c>BuildMonthOptions</c>.</summary>
    private static IReadOnlyList<MonthOption> BuildMonthOptions(int year) =>
        Enumerable.Range(
            year == MinimumSupportedYear ? MinimumSupportedMonth : 1,
            year == MinimumSupportedYear ? 13 - MinimumSupportedMonth : 12)
            .Select(month => new MonthOption(month, $"Tháng {month:00}"))
            .ToArray();

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);
        return normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth
            ? (MinimumSupportedMonth, MinimumSupportedYear)
            : (normalizedMonth, normalizedYear);
    }

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp cơm.</summary>
    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }
}
