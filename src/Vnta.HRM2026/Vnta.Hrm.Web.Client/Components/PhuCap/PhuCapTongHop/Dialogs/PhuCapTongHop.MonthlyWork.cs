using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Điều phối popup dùng chung để đối chiếu bảng công tháng theo từng dòng tổng hợp phụ cấp.</summary>
public partial class PhuCapTongHop
{
    private bool CanViewMonthlyWork(PayrollAllowanceSummaryRecord record) =>
        CanOperateOnCurrentDataset
        && !IsMonthlyWorkPopupLoading
        && record.EmployeeId != Guid.Empty;

    private async Task OpenMonthlyWorkPopupAsync(PayrollAllowanceSummaryRecord record)
    {
        if (!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Đối chiếu bảng công tháng";
        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Kỳ {record.PayrollPeriodDisplay}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
        IsMonthlyWorkPopupVisible = true;
        await LoadMonthlyWorkPopupDataAsync(record);
    }

    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if (MonthlyWorkPopupRecord is null
            || IsMonthlyWorkPopupLoading
            || !CanViewMonthlyWork(MonthlyWorkPopupRecord)
            || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);
    }

    private async Task LoadMonthlyWorkPopupDataAsync(PayrollAllowanceSummaryRecord record)
    {
        if (record.EmployeeId == Guid.Empty
            || record.PayrollMonth is < 1 or > 12
            || record.PayrollYear < 1)
        {
            return;
        }

        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;

        try
        {
            var fromDate = new DateOnly(record.PayrollYear, record.PayrollMonth, 1);
            var toDate = new DateOnly(
                record.PayrollYear,
                record.PayrollMonth,
                DateTime.DaysInMonth(record.PayrollYear, record.PayrollMonth));
            var monthlyWork = await MonthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
                fromDate,
                toDate,
                record.EmployeeId,
                disposalTokenSource.Token);

            MonthlyWorkRows = monthlyWork?.DayCellsByDate.Values
                .OrderBy(day => day.WorkDate)
                .Select(day => new MonthlyWorkdayPopupRow(
                    day.Id,
                    day.WorkDate,
                    day.DayTypeDisplay,
                    string.IsNullOrWhiteSpace(day.ShiftShortName) ? "--" : day.ShiftShortName.Trim(),
                    day.ShiftColorHex,
                    day.CheckInDisplay,
                    day.CheckOutDisplay,
                    string.IsNullOrWhiteSpace(day.Status) ? string.Empty : day.Status,
                    day.LateMinutes,
                    day.EarlyLeaveMinutes,
                    day.OvertimeMinutes,
                    day.OvertimeMinutes15,
                    day.OvertimeMinutes20,
                    day.OvertimeMinutes30,
                    day.IsLocked ? "Đã khóa" : "Mở",
                    day.IsLocked))
                .ToArray()
                ?? [];
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
            IsMonthlyWorkPopupVisible = false;
        }
        catch (Exception)
        {
            MonthlyWorkPopupErrorMessage = "Không thể tải bảng công tháng của nhân viên.";
        }
        finally
        {
            IsMonthlyWorkPopupLoading = false;
        }
    }

    private void CloseMonthlyWorkPopup()
    {
        if (IsMonthlyWorkPopupLoading)
        {
            return;
        }

        IsMonthlyWorkPopupVisible = false;
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = null;
    }
}
