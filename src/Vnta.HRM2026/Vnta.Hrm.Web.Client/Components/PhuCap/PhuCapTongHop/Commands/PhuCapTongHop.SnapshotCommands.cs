using Vnta.Hrm.Web.Client.Services.Ui;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Owns previous-period synchronization and allowance refresh workflows.</summary>
public partial class PhuCapTongHop
{
    private Task OnSyncFromPreviousMonthClick()
    {
        if(CanSyncFromPreviousMonth) IsSyncConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseSyncConfirmPopup()
    {
        if(!IsSyncingFromPreviousMonth) IsSyncConfirmPopupVisible = false;
    }

    private async Task ConfirmSyncFromPreviousMonthAsync()
    {
        if(!CanSyncFromPreviousMonth || !IsSyncConfirmPopupVisible) return;
        var targetPayrollMonth = AppliedMonth;
        var targetPayrollYear = AppliedYear;
        var sourcePeriod = GetPreviousPeriod(targetPayrollMonth, targetPayrollYear);
        var sourcePeriodDisplay = $"{sourcePeriod.Month:00}/{sourcePeriod.Year}";
        var targetPeriodDisplay = $"{targetPayrollMonth:00}/{targetPayrollYear}";
        IsSyncConfirmPopupVisible = false;
        IsSyncingFromPreviousMonth = true;
        CurrentActionLoadingText = $"Đang lấy dữ liệu từ kỳ {sourcePeriodDisplay} sang {targetPeriodDisplay}...";
        await RenderBusyStateAsync();
        try
        {
            var result = await DataProvider.SyncFromPreviousMonthAsync(targetPayrollMonth, targetPayrollYear, disposalTokenSource.Token);
            await ReloadAsync();
            if(result.AttendanceEmployeeCount == 0)
            {
                ToastService.ShowInfo($"Không có nhân viên trong dữ liệu chấm công của kỳ {targetPeriodDisplay}. Đã xóa {result.RemovedCount:N0} dòng tổng hợp không còn hợp lệ.");
                return;
            }
            ToastService.ShowSuccess($"Đã chuẩn bị {result.AttendanceEmployeeCount:N0} nhân viên có chấm công cho kỳ {targetPeriodDisplay}: lấy {result.SourceRecordCount:N0} snapshot từ kỳ {sourcePeriodDisplay}, tạo mới {result.CreatedCount:N0}, cập nhật {result.UpdatedCount:N0}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa và xóa {result.RemovedCount:N0} dòng không còn chấm công.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested) throw;
        }
        catch(Exception ex) { ToastService.ShowError(ex.Message); }
        finally { IsSyncingFromPreviousMonth = false; CurrentActionLoadingText = null; }
    }

    private Task OpenRefreshConfirmPopupAsync()
    {
        if(CanRefreshAllowances) IsRefreshConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseRefreshConfirmPopup()
    {
        if(!IsRefreshingAllowances) IsRefreshConfirmPopupVisible = false;
    }

    private async Task ConfirmRefreshAllowancesAsync()
    {
        if(!CanRefreshAllowances || !IsRefreshConfirmPopupVisible) return;
        var targetPayrollMonth = AppliedMonth;
        var targetPayrollYear = AppliedYear;
        var targetPeriodDisplay = $"{targetPayrollMonth:00}/{targetPayrollYear}";
        IsRefreshConfirmPopupVisible = false;
        IsRefreshingAllowances = true;
        CurrentActionLoadingText = $"Đang làm mới dữ liệu phụ cấp cho kỳ {targetPeriodDisplay}...";
        await RenderBusyStateAsync();
        try
        {
            var result = await DataProvider.RefreshAsync(
                targetPayrollMonth: targetPayrollMonth,
                targetPayrollYear: targetPayrollYear,
                cancellationToken: disposalTokenSource.Token);
            await ReloadAsync();
            if(result.SourceEmployeeCount == 0 && result.CreatedCount == 0 && result.UpdatedCount == 0)
            {
                ToastService.ShowInfo($"Chưa có dữ liệu phụ cấp chi tiết để làm mới cho kỳ {targetPeriodDisplay}.");
                return;
            }
            ToastService.ShowSuccess($"Đã làm mới phụ cấp kỳ {targetPeriodDisplay}: tạo mới {result.CreatedCount:N0}, cập nhật {result.UpdatedCount:N0}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa. Nguồn: Trách nhiệm {result.ResponsibilitySourceCount:N0}, Trách nhiệm khác {result.OtherResponsibilitySourceCount:N0}, Thâm niên {result.SenioritySourceCount:N0}, Chuyên cần {result.AttendanceSourceCount:N0}, Cơm {result.MealSourceCount:N0}, Độc hại {result.HazardSourceCount:N0}, Khác {result.OtherAllowanceSourceCount:N0}, Phép/Lễ {result.LeaveHolidaySourceCount:N0}.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested) throw;
        }
        catch(Exception ex) { ToastService.ShowError(ex.Message); }
        finally { IsRefreshingAllowances = false; CurrentActionLoadingText = null; }
    }

    private async Task RefreshRowAsync(PayrollAllowanceSummaryRecord row)
    {
        if(!CanRefreshRow(row)) return;
        IsRefreshingAllowances = true;
        CurrentActionLoadingText = $"Đang làm mới phụ cấp của {row.EmployeeDisplay}...";
        await RenderBusyStateAsync();
        try
        {
            var result = await DataProvider.RefreshAsync(row.PayrollMonth, row.PayrollYear, row.Id, disposalTokenSource.Token);
            await ReloadAsync();
            ToastService.ShowSuccess(result.UpdatedCount > 0 ? $"Đã làm mới phụ cấp của {row.EmployeeDisplay}." : $"Phụ cấp của {row.EmployeeDisplay} đã là dữ liệu mới nhất.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested) throw;
        }
        catch(Exception ex) { ToastService.ShowError(ex.Message); }
        finally { IsRefreshingAllowances = false; CurrentActionLoadingText = null; }
    }
}
