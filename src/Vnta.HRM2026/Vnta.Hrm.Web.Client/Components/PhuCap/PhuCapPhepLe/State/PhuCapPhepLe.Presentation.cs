using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLe
{
    #region Display Helpers

    /// <summary>Lấy cho luồng <c>GetLockStatusCssClass</c>.</summary>
    private static string GetLockStatusCssClass(bool isLocked) => string.Join(
        ' ',
        "yes-no-status",
        isLocked ? "yes-no-status-no" : "yes-no-status-yes");

    /// <summary>Lấy cho luồng <c>GetAuditActorDisplay</c>.</summary>
    private static string GetAuditActorDisplay(LeaveHolidayAllowanceRecord row) =>
        string.IsNullOrWhiteSpace(row.UpdatedBy)
            ? row.CreatedBy
            : row.UpdatedBy!;

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanEditRow</c>.</summary>
    private bool CanEditRow(LeaveHolidayAllowanceRecord row) =>
        CanOperateOnCurrentDataset && !row.IsLocked;

    /// <summary>Kiểm tra dòng có được phép tính lại riêng lẻ hay không.</summary>
    private bool CanRefreshRow(LeaveHolidayAllowanceRecord row) =>
        CanOperateOnCurrentDataset && !row.IsLocked;

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanToggleLock</c>.</summary>
    private bool CanToggleLock(LeaveHolidayAllowanceRecord row) =>
        CanOperateOnCurrentDataset;

    /// <summary>Kiểm tra điều kiện cho luồng <c>CanViewMonthlyWork</c>.</summary>
    private bool CanViewMonthlyWork(LeaveHolidayAllowanceRecord row) =>
        CanOperateOnCurrentDataset && row.EmployeeId != Guid.Empty;

    /// <summary>Định dạng cho luồng <c>FormatMoneyCell</c>.</summary>
    private string FormatMoneyCell(decimal value) => FormatMoney(value);

    /// <summary>Định dạng cho luồng <c>FormatQuantityCell</c>.</summary>
    private string FormatQuantityCell(decimal value) => value == 0m ? string.Empty : FormatQuantity(value);

    /// <summary>Định dạng cho luồng <c>FormatMoney</c>.</summary>
    private string FormatMoney(decimal value) =>
        value == 0m ? string.Empty : string.Format(DisplayCulture, "{0:N0} đ", value);

    /// <summary>Định dạng cho luồng <c>FormatQuantity</c>.</summary>
    private string FormatQuantity(decimal value) => string.Format(DisplayCulture, "{0:N2}", value);

    /// <summary>Định dạng cho luồng <c>FormatAuditDate</c>.</summary>
    private string FormatAuditDate(DateTime? value) =>
        value.HasValue
            ? value.Value.ToString("dd/MM/yyyy HH:mm", DisplayCulture)
            : "Chưa cập nhật";

    /// <summary>Lấy cho luồng <c>GetNotePreview</c>.</summary>
    private string GetNotePreview(string? note) =>
        string.IsNullOrWhiteSpace(note)
            ? string.Empty
            : note.Length <= 80
                ? note
                : $"{note[..77]}...";

    /// <summary>Thực hiện xử lý cho luồng <c>HighlightSearchText</c>.</summary>
    private MarkupString HighlightSearchText(string? value)
    {
        var displayText = string.IsNullOrWhiteSpace(value) ? "--" : value.Trim();
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var searchText = SearchText.Trim();
        if (searchText.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        var startIndex = 0;
        var builder = new StringBuilder(displayText.Length + 32);
        while (true)
        {
            var matchIndex = displayText.IndexOf(searchText, startIndex, StringComparison.OrdinalIgnoreCase);
            if (matchIndex < 0)
            {
                break;
            }

            builder.Append(WebUtility.HtmlEncode(displayText[startIndex..matchIndex]));
            builder.Append("<mark class=\"responsibility-search-highlight\">");
            builder.Append(WebUtility.HtmlEncode(displayText.Substring(matchIndex, searchText.Length)));
            builder.Append("</mark>");
            startIndex = matchIndex + searchText.Length;
        }

        if (builder.Length == 0)
        {
            return new MarkupString(WebUtility.HtmlEncode(displayText));
        }

        builder.Append(WebUtility.HtmlEncode(displayText[startIndex..]));
        return new MarkupString(builder.ToString());
    }

    #endregion

    #region Toast Helpers

    /// <summary>Thực hiện xử lý cho luồng <c>ShowRecalculateToast</c>.</summary>
    private void ShowRecalculateToast(
        RecalculateLeaveHolidayAllowanceResult result,
        string payrollPeriod)
    {
        if (result.UpdatedCount == 0)
        {
            if (result.TotalRowCount == 0)
            {
                ToastService.ShowInfo($"Kỳ {payrollPeriod} hiện chưa có dữ liệu phụ cấp Phép - Lễ để tính lại.");
                return;
            }

            if (result.SkippedLockedCount >= result.TotalRowCount)
            {
                ToastService.ShowInfo($"Kỳ {payrollPeriod} hiện không có dòng mở để tính lại.");
                return;
            }

            ToastService.ShowInfo($"Kỳ {payrollPeriod} hiện không có dòng nào cần tính lại.");
            return;
        }

        ToastService.ShowSuccess(
            $"Đã tính lại phụ cấp Phép - Lễ kỳ {payrollPeriod}: cập nhật {result.UpdatedCount:N0} dòng, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
    }

    /// <summary>Tạo cho luồng <c>BuildBatchLockStateSuccessMessage</c>.</summary>
    private static string BuildBatchLockStateSuccessMessage(
        bool shouldLock,
        string payrollPeriod,
        int targetRowCount,
        int updatedCount,
        int skippedCount,
        bool isWholePeriod)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var unchangedCount = Math.Max(0, targetRowCount - updatedCount);
        var scopeText = isWholePeriod ? "toàn bộ kỳ" : "các dòng đã chọn";
        var skippedText = skippedCount > 0
            ? $", bỏ qua {skippedCount:N0} ID không tồn tại hoặc không thuộc kỳ"
            : string.Empty;

        return unchangedCount > 0
            ? $"Đã {actionText} {updatedCount:N0}/{targetRowCount:N0} dòng phụ cấp Phép - Lễ trong phạm vi {scopeText} của kỳ {payrollPeriod}, giữ nguyên {unchangedCount:N0} dòng đã đúng trạng thái{skippedText}."
            : $"Đã {actionText} {updatedCount:N0} dòng phụ cấp Phép - Lễ trong phạm vi {scopeText} của kỳ {payrollPeriod}{skippedText}.";
    }

    /// <summary>Định dạng cho luồng <c>FormatBatchSkippedCount</c>.</summary>
    private static string FormatBatchSkippedCount(int skippedCount) =>
        skippedCount > 0
            ? $", bỏ qua {skippedCount:N0} ID không tồn tại hoặc không thuộc kỳ"
            : string.Empty;

    #endregion

    #region Static Helpers

    /// <summary>Chuẩn hóa cho luồng <c>NormalizeOptional</c>.</summary>
    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Lấy cho luồng <c>GetNoteCssClass</c>.</summary>
    private static string GetNoteCssClass(string? note) =>
        string.Equals(
            NormalizeOptional(note),
            MissingBasicSalaryReferenceNote,
            StringComparison.Ordinal)
            ? "leave-holiday-allowance-grid-text leave-holiday-allowance-warning-note"
            : "leave-holiday-allowance-grid-text";

    /// <summary>Định dạng cho luồng <c>FormatPayrollPeriod</c>.</summary>
    private static string FormatPayrollPeriod(int payrollMonth, int payrollYear) => $"{payrollMonth:00}/{payrollYear}";

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private static (int Month, int Year) NormalizeSelectedPeriod(int month, int year)
    {
        var normalizedYear = Math.Clamp(year, MinimumSupportedYear, MaximumSupportedYear);
        var normalizedMonth = Math.Clamp(month, 1, 12);

        if (normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth)
        {
            return (MinimumSupportedMonth, MinimumSupportedYear);
        }

        return (normalizedMonth, normalizedYear);
    }

    /// <summary>Thành viên hỗ trợ xử lý dữ liệu của màn hình phụ cấp phép lễ.</summary>
    private static (int Month, int Year) GetDefaultPayrollPeriod()
    {
        var localNow = DateTime.UtcNow.AddHours(7);
        return NormalizeSelectedPeriod(localNow.Month, localNow.Year);
    }

    #endregion
}
