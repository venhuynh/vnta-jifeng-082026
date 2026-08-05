using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

public sealed partial class OtherAllowanceCoordinator
{
    private bool CanViewMonthlyWorkRow(OtherAllowanceListItemDto row) =>
        CanOperateOnCurrentDataset && row.EmployeeId != Guid.Empty && IsValidPayrollPeriod(row.PayrollMonth, row.PayrollYear);

    private async Task OpenMonthlyWorkPopupAsync(OtherAllowanceListItemDto row)
    {
        if(!CanViewMonthlyWorkRow(row) || disposalTokenSource.IsCancellationRequested) return;
        MonthlyWorkPopupTitle = "Đối chiếu bảng công tháng";
        MonthlyWorkPopupContext = $"{GetEmployeeDisplay(row)} - Kỳ {row.PayrollMonth:00}/{row.PayrollYear}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = row;
        IsMonthlyWorkPopupVisible = true;
        await LoadMonthlyWorkPopupDataAsync(row);
    }

    private Task RefreshMonthlyWorkPopupAsync() => MonthlyWorkPopupRecord is null || IsMonthlyWorkPopupLoading || !CanViewMonthlyWorkRow(MonthlyWorkPopupRecord) || disposalTokenSource.IsCancellationRequested
        ? Task.CompletedTask : LoadMonthlyWorkPopupDataAsync(MonthlyWorkPopupRecord);

    private async Task LoadMonthlyWorkPopupDataAsync(OtherAllowanceListItemDto row)
    {
        if(!CanViewMonthlyWorkRow(row)) return;
        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;
        try
        {
            var fromDate = new DateOnly(row.PayrollYear, row.PayrollMonth, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);
            var monthlyWork = await MonthlyWorkDataProvider.LoadEmployeeMonthlyWorkAsync(fromDate, toDate, row.EmployeeId, disposalTokenSource.Token);
            MonthlyWorkRows = monthlyWork?.DayCellsByDate.Values.OrderBy(day => day.WorkDate).Select(day => new MonthlyWorkdayPopupRow(
                day.Id, day.WorkDate, day.DayTypeDisplay,
                string.IsNullOrWhiteSpace(day.ShiftShortName) ? "--" : day.ShiftShortName.Trim(), day.ShiftColorHex,
                day.CheckInDisplay, day.CheckOutDisplay,
                string.IsNullOrWhiteSpace(day.Status) ? "--" : day.Status.Trim(), day.LateMinutes, day.EarlyLeaveMinutes,
                day.OvertimeMinutes, day.OvertimeMinutes15, day.OvertimeMinutes20, day.OvertimeMinutes30,
                day.IsLocked ? "Đã khóa" : "Mở", day.IsLocked)).ToArray() ?? [];
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { IsMonthlyWorkPopupVisible = false; }
        catch(UnauthorizedAccessException)
        {
            MonthlyWorkPopupErrorMessage = "Bạn không có quyền xem bảng công tháng.";
            ToastService.ShowWarning(MonthlyWorkPopupErrorMessage);
        }
        catch(Exception exception)
        {
            Logger.LogError(exception, "Không thể tải bảng công tháng cho phụ cấp khác kỳ {PayrollMonth}/{PayrollYear}.", row.PayrollMonth, row.PayrollYear);
            MonthlyWorkPopupErrorMessage = "Không thể tải bảng công tháng của nhân viên.";
            ToastService.ShowError(MonthlyWorkPopupErrorMessage);
        }
        finally { IsMonthlyWorkPopupLoading = false; }
    }

    private void CloseMonthlyWorkPopup()
    {
        if(IsMonthlyWorkPopupLoading) return;
        IsMonthlyWorkPopupVisible = false;
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkPopupContext = string.Empty;
        MonthlyWorkPopupRecord = null;
        MonthlyWorkRows = [];
    }
}
