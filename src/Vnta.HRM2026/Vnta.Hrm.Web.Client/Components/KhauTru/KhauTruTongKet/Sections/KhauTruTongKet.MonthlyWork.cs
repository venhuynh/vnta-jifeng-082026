using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;

public partial class KhauTruTongKet
{
    private int monthlyWorkPopupRequestVersion;

    private async Task OpenMonthlyWorkPopupAsync(PayrollDeductionSummaryRecord record)
    {
        if(!CanViewMonthlyWork(record) || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        MonthlyWorkPopupTitle = "Đối chiếu bảng công chi tiết";
        MonthlyWorkPopupContext =
            $"{record.EmployeeDisplay} - {record.DepartmentDisplay} - {record.PositionDisplay} - Tháng {CurrentPayrollPeriodDisplay}";
        MonthlyWorkPopupErrorMessage = null;
        MonthlyWorkRows = [];
        MonthlyWorkPopupRecord = record;
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

    private async Task LoadMonthlyWorkPopupDataAsync(PayrollDeductionSummaryRecord record)
    {
        // Chỉ request mới nhất của popup hiện tại được phép cập nhật state sau khi bất đồng bộ hoàn tất.
        var requestVersion = Interlocked.Increment(ref monthlyWorkPopupRequestVersion);
        IsMonthlyWorkPopupLoading = true;
        MonthlyWorkPopupErrorMessage = null;
        await InvokeAsync(StateHasChanged);
        await Task.Yield();

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

            if(!CanApplyMonthlyWorkPopupResult(requestVersion, record))
            {
                return;
            }

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
        }
        catch(UnauthorizedAccessException)
        {
            if(CanApplyMonthlyWorkPopupResult(requestVersion, record))
            {
                MonthlyWorkPopupErrorMessage = "Bạn không có quyền xem bảng công tháng.";
            }
        }
        catch(Exception)
        {
            if(CanApplyMonthlyWorkPopupResult(requestVersion, record))
            {
                MonthlyWorkPopupErrorMessage = "Không thể tải bảng công tháng của nhân viên.";
            }
        }
        finally
        {
            if(CanApplyMonthlyWorkPopupResult(requestVersion, record))
            {
                IsMonthlyWorkPopupLoading = false;
            }
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

    private bool CanViewMonthlyWork(PayrollDeductionSummaryRecord record) =>
        CanOperateOnCurrentDataset
        && CanReadMonthlyWork
        && !IsMonthlyWorkPopupLoading
        && record.EmployeeId != Guid.Empty
        && record.PayrollMonth == AppliedMonth
        && record.PayrollYear == AppliedYear;

    private bool CanApplyMonthlyWorkPopupResult(int requestVersion, PayrollDeductionSummaryRecord record) =>
        !isDisposed
        && IsMonthlyWorkPopupVisible
        && requestVersion == Volatile.Read(ref monthlyWorkPopupRequestVersion)
        && MonthlyWorkPopupRecord?.Id == record.Id;
}
