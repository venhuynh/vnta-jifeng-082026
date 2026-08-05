using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Điều phối popup đối chiếu bảng công chỉ đọc của Phụ cấp cơm.</summary>
public partial class PhuCapCom
{
    private async Task OpenMonthlyWorkPopupAsync(MealAllowanceRecord record)
    {
        if(!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Đối chiếu bảng công tháng";
        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Tháng {AppliedPayrollPeriodDisplay}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
        IsMonthlyWorkPopupVisible = true;
        await LoadMonthlyWorkPopupDataAsync(record);
    }

    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if(MonthlyWorkPopupRecord is null
            || IsMonthlyWorkPopupLoading
            || !CanViewMonthlyWork(MonthlyWorkPopupRecord)
            || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);
    }

    private async Task LoadMonthlyWorkPopupDataAsync(MealAllowanceRecord record)
    {
        if(record.EmployeeId is not { } employeeId || employeeId == Guid.Empty)
        {
            return;
        }

        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;

        try
        {
            var (payrollMonth, payrollYear) = GetAppliedPayrollPeriod();
            var fromDate = new DateOnly(payrollYear, payrollMonth, 1);
            var toDate = new DateOnly(
                payrollYear,
                payrollMonth,
                DateTime.DaysInMonth(payrollYear, payrollMonth));
            var monthlyWork = await MonthlyWorkSummaryDataProvider.LoadEmployeeMonthAsync(
                fromDate,
                toDate,
                employeeId,
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
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            IsMonthlyWorkPopupVisible = false;
        }
        catch(Exception)
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
        if(IsMonthlyWorkPopupLoading)
        {
            return;
        }

        IsMonthlyWorkPopupVisible = false;
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = null;
    }
}
