using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruKhac;

public partial class KhauTruKhac
{
    private async Task OpenMonthlyWorkPopupAsync(KhauTruKhacRecord record)
    {
        if(!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Đối chiếu bảng công chi tiết";
        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Tháng {AppliedPeriodLabel}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
        MonthlyWorkPopupSalaryWorkDays = record.SalaryWorkDays ?? 0m;
        IsMonthlyWorkPopupVisible = true;
        await LoadMonthlyWorkPopupDataAsync(record);
    }

    private async Task RefreshMonthlyWorkPopupAsync()
    {
        if(MonthlyWorkPopupRecord is null || IsMonthlyWorkPopupLoading || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        await LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);
    }

    private async Task LoadMonthlyWorkPopupDataAsync(KhauTruKhacRecord record)
    {
        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;

        try
        {
            var fromDate = new DateOnly(AppliedYear, AppliedMonth, 1);
            var toDate = new DateOnly(
                AppliedYear,
                AppliedMonth,
                DateTime.DaysInMonth(AppliedYear, AppliedMonth));
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
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            IsMonthlyWorkPopupVisible = false;
        }
        catch(Exception)
        {
            MonthlyWorkPopupErrorMessage = "Không thể tải chi tiết khấu trừ khác của nhân viên.";
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
        MonthlyWorkPopupSalaryWorkDays = 0m;
    }
}
