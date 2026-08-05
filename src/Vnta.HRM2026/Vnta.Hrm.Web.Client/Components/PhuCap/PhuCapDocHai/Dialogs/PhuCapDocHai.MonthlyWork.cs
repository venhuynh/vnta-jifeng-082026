using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;
using Vnta.Hrm.Web.Client.Components.Shared.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHai</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHai
{
    #region Popup bảng công tháng

    /// <summary>Mở popup chỉ đọc và tải bảng công của đúng nhân viên/kỳ thuộc row được chọn.</summary>
    private async Task OpenMonthlyWorkPopupAsync(HazardAllowanceListItemDto record)
    {
        if(!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Đối chiếu bảng công tháng";
        MonthlyWorkPopupContext = $"{GetEmployeeDisplay(record)} - Kỳ {FormatPayrollPeriod(record.PayrollMonth, record.PayrollYear)}";
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
        IsMonthlyWorkPopupVisible = true;
        await LoadMonthlyWorkPopupDataAsync(record);
    }

    /// <summary>Tải lại popup đang mở, không chạy khi popup không có record hoặc đang tải.</summary>
    private Task RefreshMonthlyWorkPopupAsync() =>
        MonthlyWorkPopupRecord is null || IsMonthlyWorkPopupLoading || disposalTokenSource.IsCancellationRequested
            ? Task.CompletedTask
            : LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);

    /// <summary>Map day-cell nguồn thành read model popup, giữ nguyên thứ tự ngày công.</summary>
    private async Task LoadMonthlyWorkPopupDataAsync(HazardAllowanceListItemDto record)
    {
        IsMonthlyWorkPopupLoading = true;
        try
        {
            var fromDate = new DateOnly(record.PayrollYear, record.PayrollMonth, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);
            var monthlyWork = await ExecuteDataOperationAsync(
                cancellationToken => MonthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
                    fromDate,
                    toDate,
                    record.EmployeeId,
                    cancellationToken));

            MonthlyWorkRows = monthlyWork?.DayCellsByDate.Values
                .OrderBy(day => day.WorkDate)
                .Select(day => new MonthlyWorkdayPopupRow(
                    day.Id,
                    day.WorkDate,
                    day.DayTypeDisplay,
                    string.IsNullOrWhiteSpace(day.ShiftShortName) ? "--" : day.ShiftShortName.Trim(),
                    null,
                    day.CheckInDisplay,
                    day.CheckOutDisplay,
                    string.IsNullOrWhiteSpace(day.Status) ? "--" : day.Status.Trim(),
                    day.LateMinutes,
                    day.EarlyLeaveMinutes,
                    day.OvertimeMinutes15 + day.OvertimeMinutes20 + day.OvertimeMinutes30,
                    day.OvertimeMinutes15,
                    day.OvertimeMinutes20,
                    day.OvertimeMinutes30,
                    day.IsLocked ? "Đã khóa" : "Mở",
                    day.IsLocked))
                .ToArray()
                ?? [];
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            IsMonthlyWorkPopupVisible = false;
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể tải bảng công tháng cho phụ cấp độc hại kỳ {PayrollMonth}/{PayrollYear}.", record.PayrollMonth, record.PayrollYear);
            IsMonthlyWorkPopupVisible = false;
            ToastService.ShowError("Không thể tải bảng công tháng của nhân viên.");
        }
        finally
        {
            IsMonthlyWorkPopupLoading = false;
        }
    }

    /// <summary>Đóng popup sau khi hoàn tất tải và giải phóng state chi tiết của row cũ.</summary>
    private void CloseMonthlyWorkPopup()
    {
        if(IsMonthlyWorkPopupLoading)
        {
            return;
        }

        IsMonthlyWorkPopupVisible = false;
        MonthlyWorkPopupRecord = null;
        MonthlyWorkRows = [];
    }

    #endregion
}
