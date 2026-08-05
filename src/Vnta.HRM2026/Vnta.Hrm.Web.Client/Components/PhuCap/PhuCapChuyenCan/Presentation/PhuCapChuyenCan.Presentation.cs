using System.Net;
using System.Text;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Export;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan;

public partial class PhuCapChuyenCan
{
    /// <summary>Tạo cho luồng <c>BuildSummaryBadges</c>.</summary>
    private static IReadOnlyList<AttendanceAllowanceSummaryBadge> BuildSummaryBadges(
        int openCount,
        int lockedCount,
        int attendanceClassACount,
        int attendanceClassBCount,
        int attendanceClassCCount) =>
    [
        new(SummaryAllKey, "Tất cả", "Tất cả", openCount + lockedCount),
        new(SummaryAttendanceClassAKey, "CC A", "CC A", attendanceClassACount),
        new(SummaryAttendanceClassBKey, "CC B", "CC B", attendanceClassBCount),
        new(SummaryAttendanceClassCKey, "CC C", "CC C", attendanceClassCCount),
        new(SummaryLockedKey, "Đã khóa", "Đã khóa", lockedCount),
        new(SummaryOpenKey, "Chưa khóa", "Chưa khóa", openCount)
    ];

    /// <summary>Lấy cho luồng <c>GetLockStateForBadge</c>.</summary>
    private static AttendanceAllowanceLockState GetLockStateForBadge(string badgeKey) => badgeKey switch
    {
        SummaryOpenKey => AttendanceAllowanceLockState.Open,
        SummaryLockedKey => AttendanceAllowanceLockState.Locked,
        _ => AttendanceAllowanceLockState.All
    };

    /// <summary>Lấy cho luồng <c>GetAttendanceClassForBadge</c>.</summary>
    private static string? GetAttendanceClassForBadge(string badgeKey) => badgeKey switch
    {
        SummaryAttendanceClassAKey => AttendanceAllowanceClass.A.ToStorageValue(),
        SummaryAttendanceClassBKey => AttendanceAllowanceClass.B.ToStorageValue(),
        SummaryAttendanceClassCKey => AttendanceAllowanceClass.C.ToStorageValue(),
        _ => null
    };

    /// <summary>Thực hiện xử lý cho luồng <c>HighlightSearchText</c>.</summary>
    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = value?.Trim() ?? string.Empty;
        if(string.IsNullOrWhiteSpace(SearchText) || displayText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        var sourceIndex = 0;
        var builder = new StringBuilder();

        while(sourceIndex < displayText.Length)
        {
            var matchIndex = displayText.IndexOf(searchText, sourceIndex, StringComparison.OrdinalIgnoreCase);
            if(matchIndex < 0)
            {
                builder.Append(WebUtility.HtmlEncode(displayText[sourceIndex..]));
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[sourceIndex..matchIndex]));
            builder.Append("<mark>");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            sourceIndex = matchIndex + searchText.Length;
        }

        return new MarkupString(builder.ToString());
    }

    /// <summary>Lấy cho luồng <c>GetLockBadgeCssClass</c>.</summary>
    private static string GetLockBadgeCssClass(bool isLocked) => isLocked
        ? "yes-no-status yes-no-status-no hrm-grid-status"
        : "yes-no-status yes-no-status-yes hrm-grid-status";

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeNullable</c>.</summary>
    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Thực hiện xử lý cho luồng <c>ShowRefreshResultToast</c>.</summary>
    private void ShowRefreshResultToast(RefreshAttendanceAllowanceResult result)
    {
        var payrollPeriod = FormatPayrollPeriod(result.PayrollMonth, result.PayrollYear);
        ToastService.ShowSuccess(
            $"Đã tính lại dữ liệu phụ cấp chuyên cần kỳ {payrollPeriod}: khớp {result.MatchedRowCount:N0} dòng, cập nhật {result.UpdatedCount:N0}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
    }

    /// <summary>Lấy cho luồng <c>GetLockStateLoadingMessage</c>.</summary>
    private static string GetLockStateLoadingMessage(bool shouldLock) => shouldLock
        ? "Đang khóa dữ liệu phụ cấp chuyên cần..."
        : "Đang mở khóa dữ liệu phụ cấp chuyên cần...";

    /// <summary>Tạo cho luồng <c>BuildLockStateSuccessMessage</c>.</summary>
    private static string BuildLockStateSuccessMessage(bool shouldLock, int affectedCount) => shouldLock
        ? affectedCount == 1
            ? "Đã khóa dòng phụ cấp chuyên cần đã chọn."
            : $"Đã khóa {affectedCount:N0} dòng phụ cấp chuyên cần đã chọn."
        : affectedCount == 1
            ? "Đã mở khóa dòng phụ cấp chuyên cần đã chọn."
            : $"Đã mở khóa {affectedCount:N0} dòng phụ cấp chuyên cần đã chọn.";

    /// <summary>Tạo cho luồng <c>BuildLockStateFailureMessage</c>.</summary>
    private static string BuildLockStateFailureMessage(bool shouldLock) => shouldLock
        ? "Không thể khóa các dòng phụ cấp chuyên cần đã chọn."
        : "Không thể mở khóa các dòng phụ cấp chuyên cần đã chọn.";

    /// <summary>Tạo cho luồng <c>BuildLockStateNoDataMessage</c>.</summary>
    private static string BuildLockStateNoDataMessage(bool shouldLock) => shouldLock
        ? "Không có dòng phụ cấp chuyên cần nào phù hợp để khóa trong kỳ đang thao tác."
        : "Không có dòng phụ cấp chuyên cần nào phù hợp để mở khóa trong kỳ đang thao tác.";

    /// <summary>Tạo cho luồng <c>BuildLockStateNoEligibleRowsMessage</c>.</summary>
    private static string BuildLockStateNoEligibleRowsMessage(bool shouldLock, int targetRowCount) => shouldLock
        ? targetRowCount == 1
            ? "Dòng phụ cấp chuyên cần đã chọn không cần khóa."
            : $"Có {targetRowCount:N0} dòng đã chọn nhưng không có dòng nào cần khóa."
        : targetRowCount == 1
            ? "Dòng phụ cấp chuyên cần đã chọn không cần mở khóa."
            : $"Có {targetRowCount:N0} dòng đã chọn nhưng không có dòng nào cần mở khóa.";

    /// <summary>Kiểm tra trạng thái cho luồng <c>IsWholePeriodLockStateScope</c>.</summary>
    private static bool IsWholePeriodLockStateScope(string scope) =>
        string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal);

    /// <summary>Tạo cho luồng <c>BuildLockStateSuccessMessage</c>.</summary>
    private static string BuildLockStateSuccessMessage(
        bool shouldLock,
        string scope,
        SetAttendanceAllowanceBatchLockStateResult result,
        int payrollMonth,
        int payrollYear)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var payrollPeriod = FormatPayrollPeriod(payrollMonth, payrollYear);
        if(IsWholePeriodLockStateScope(scope))
        {
            var details = new List<string>();
            if(result.UnchangedCount > 0)
            {
                details.Add($"giữ nguyên {result.UnchangedCount:N0} dòng đã đúng trạng thái");
            }

            if(result.SkippedSummaryLockedCount > 0)
            {
                details.Add($"bỏ qua {result.SkippedSummaryLockedCount:N0} dòng có summary đã khóa");
            }

            var detailText = details.Count == 0 ? string.Empty : $", {string.Join(", ", details)}";
            return $"Đã {actionText} {result.UpdatedCount:N0}/{result.TargetRowCount:N0} dòng phụ cấp chuyên cần của kỳ {payrollPeriod}{detailText}.";
        }

        return BuildLockStateSuccessMessage(shouldLock, result.UpdatedCount);
    }

    /// <summary>Tạo cho luồng <c>BuildLockStateNoDataMessage</c>.</summary>
    private static string BuildLockStateNoDataMessage(
        bool shouldLock,
        string scope,
        int payrollMonth,
        int payrollYear) =>
        IsWholePeriodLockStateScope(scope)
            ? $"Không có dữ liệu phụ cấp chuyên cần của kỳ {FormatPayrollPeriod(payrollMonth, payrollYear)} để {(shouldLock ? "khóa" : "mở khóa")}."
            : BuildLockStateNoDataMessage(shouldLock);

    /// <summary>Tạo cho luồng <c>BuildLockStateNoEligibleRowsMessage</c>.</summary>
    private static string BuildLockStateNoEligibleRowsMessage(
        bool shouldLock,
        string scope,
        SetAttendanceAllowanceBatchLockStateResult result)
    {
        if(IsWholePeriodLockStateScope(scope))
        {
            return result.SkippedSummaryLockedCount > 0
                ? $"Không có dòng nào được {(shouldLock ? "khóa" : "mở khóa")}. {result.UnchangedCount:N0} dòng đã đúng trạng thái và {result.SkippedSummaryLockedCount:N0} dòng bị summary đã khóa bảo vệ."
                : $"Không có dòng nào cần {(shouldLock ? "khóa" : "mở khóa")}. {result.UnchangedCount:N0} dòng của kỳ đã ở trạng thái phù hợp.";
        }

        return result.SkippedSummaryLockedCount > 0
            ? $"Không có dòng nào được {(shouldLock ? "khóa" : "mở khóa")}; {result.UnchangedCount:N0} dòng đã đúng trạng thái và {result.SkippedSummaryLockedCount:N0} dòng bị summary đã khóa bảo vệ."
            : BuildLockStateNoEligibleRowsMessage(shouldLock, result.TargetRowCount);
    }

    /// <summary>Tạo cho luồng <c>BuildWholePeriodScopeDescription</c>.</summary>
    private static string BuildWholePeriodScopeDescription(
        bool shouldLock,
        string payrollPeriodLabel,
        int totalCount,
        int canLockCount,
        int canUnlockCount,
        int summaryLockedCount)
    {
        if(totalCount == 0)
        {
            return $"Kỳ {payrollPeriodLabel} chưa có dòng phụ cấp chuyên cần để xử lý.";
        }

        var actionableCount = shouldLock ? canLockCount : canUnlockCount;
        var unchangedCount = Math.Max(totalCount - actionableCount - summaryLockedCount, 0);
        var actionText = shouldLock ? "khóa" : "mở khóa";
        return $"Kỳ {payrollPeriodLabel} có {totalCount:N0} dòng: {actionableCount:N0} dòng dự kiến được {actionText}, {unchangedCount:N0} dòng đã đúng trạng thái và {summaryLockedCount:N0} dòng bị summary đã khóa bảo vệ.";
    }

    /// <summary>Lấy cho luồng <c>GetAttendanceSummary</c>.</summary>
    private string GetAttendanceSummary(AttendanceAllowanceResultRecord row) =>
        $"{FormatWorkday(row.ActualWorkdayCount)} / {FormatWorkday(row.StandardWorkdayCount)} {FormatRate(row.AttendanceRate)}";

    /// <summary>Tạo tên tệp ổn định từ kỳ đã áp dụng, không dùng input toolbar chưa được tải.</summary>
    private string GetRuleSummary(AttendanceAllowanceResultRecord row) =>
        $"{FormatRuleWorkday(row.CtlWorkdayCount)}/{FormatWorkday(row.StandardWorkdayCount)} {FormatRate(row.AttendanceRate)}";

    private string FormatRuleWorkday(decimal? value) => (value ?? 0m).ToString("0.000", DisplayCulture);

    private string BuildExportFileName() => $"phu-cap-chuyen-can-{AppliedYear}-{AppliedMonth:00}";

    /// <summary>Lấy thành phần nguồn xuất tệp sau khi hoàn tất render.</summary>
    private PhuCapChuyenCanExportGrid GetExportSource() => ExportSource
        ?? throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");

    /// <summary>Định dạng cho luồng <c>FormatMoney</c>.</summary>
    private string FormatMoney(decimal value) =>
        value == 0m ? string.Empty : $"{value.ToString("N0", DisplayCulture)} đ";

    /// <summary>Định dạng cho luồng <c>FormatWorkday</c>.</summary>
    private string FormatWorkday(decimal value) => value.ToString("0.0", DisplayCulture);

    /// <summary>Định dạng cho luồng <c>FormatRate</c>.</summary>
    private string FormatRate(decimal value) => value.ToString("P1", DisplayCulture);

    /// <summary>Định dạng cho luồng <c>FormatPayrollPeriod</c>.</summary>
    private static string FormatPayrollPeriod(int payrollMonth, int payrollYear) => $"{payrollMonth:00}/{payrollYear}";

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedMonth = Math.Clamp(month, 1, 12);
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);

        if(normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth)
        {
            return (MinimumSupportedMonth, MinimumSupportedYear);
        }

        return (normalizedMonth, normalizedYear);
    }

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu phụ cấp chuyên cần.</summary>
    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }
}
