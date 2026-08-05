using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Owns row and batch lock-state workflows, including concurrency tokens.</summary>
public partial class PhuCapTongHop
{
    private async Task ToggleLockAsync(PayrollAllowanceSummaryRecord row)
    {
        if(disposalTokenSource.IsCancellationRequested || !CanToggleLock(row)) return;
        var nextLockedState = !row.IsLocked;

        IsTogglingLock = true;
        CurrentActionLoadingText = row.IsLocked ? $"Đang mở khóa dữ liệu của {row.EmployeeDisplay}..." : $"Đang khóa dữ liệu của {row.EmployeeDisplay}...";
        await RenderBusyStateAsync();
        try
        {
            await DataProvider.SetLockStateAsync(row.Id, nextLockedState, row.UpdatedAtUtc, disposalTokenSource.Token);
            await ReloadAsync();
            ToastService.ShowSuccess(nextLockedState ? $"Đã khóa dòng tổng hợp phụ cấp của {row.EmployeeDisplay}." : $"Đã mở khóa dòng tổng hợp phụ cấp của {row.EmployeeDisplay}.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested) throw;
        }
        catch(Exception ex) { ToastService.ShowError(ex.Message); }
        finally { IsTogglingLock = false; CurrentActionLoadingText = null; }
    }

    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);
    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if(!CanOperateOnCurrentDataset) return Task.CompletedTask;
        PendingLockActionState = shouldLock;
        PendingLockActionMonth = AppliedMonth;
        PendingLockActionYear = AppliedYear;
        SelectedLockActionScope = CanChooseSelectedRowsScope ? LockScopeSelectedRows : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseLockActionPopup()
    {
        if(!IsTogglingLock) IsLockActionPopupVisible = false;
    }

    private void SelectLockActionScope(string scope)
    {
        if(IsTogglingLock) return;
        if(string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal) && CanChooseSelectedRowsScope)
        {
            SelectedLockActionScope = LockScopeSelectedRows;
        }
        else if(string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }
    }

    private async Task ConfirmLockActionAsync()
    {
        var shouldLock = PendingLockActionState;
        var actionScope = SelectedLockActionScope;
        if(!CanConfirmLockAction) return;

        Guid[]? targetRecordIds = null;
        PayrollAllowanceSummaryLockStateConcurrencyToken[]? concurrencyTokens = null;
        var selectedRowCount = 0;
        if(!IsWholePeriodLockActionScope(actionScope))
        {
            var selectedRows = GetSelectedRows().Where(row => row.Id != Guid.Empty).DistinctBy(row => row.Id).ToArray();
            if(selectedRows.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }

            targetRecordIds = selectedRows.Select(row => row.Id).ToArray();
            concurrencyTokens = selectedRows.Select(row => new PayrollAllowanceSummaryLockStateConcurrencyToken(row.Id, row.UpdatedAtUtc)).ToArray();
            selectedRowCount = targetRecordIds.Length;
            if(shouldLock && IsManualEditPopupVisible && ManualEditModel is not null && targetRecordIds.Contains(ManualEditModel.Id)) CloseManualEditPopup();
        }
        else if(shouldLock && IsManualEditPopupVisible)
        {
            CloseManualEditPopup();
        }

        var actionText = shouldLock ? "khóa" : "mở khóa";
        try
        {
            IsTogglingLock = true;
            IsLockActionPopupVisible = false;
            CurrentActionLoadingText = IsWholePeriodLockActionScope(actionScope)
                ? $"Đang {actionText} dữ liệu tổng hợp phụ cấp của toàn bộ kỳ {PendingLockActionPeriodDisplay}..."
                : $"Đang {actionText} {selectedRowCount:N0} dòng tổng hợp phụ cấp đã chọn...";
            await RenderBusyStateAsync();
            var result = await DataProvider.SetLockStateBatchAsync(
                new SetPayrollAllowanceSummaryBatchLockStateRequest(PendingLockActionYear, PendingLockActionMonth, shouldLock, targetRecordIds, concurrencyTokens),
                disposalTokenSource.Token);
            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu tổng hợp phụ cấp của kỳ {PendingLockActionPeriodDisplay} để {actionText}.");
                return;
            }
            if(result.UpdatedCount == 0)
            {
                ToastService.ShowInfo($"Không có dòng nào cần {actionText}; {result.TargetRowCount:N0} dòng đã ở đúng trạng thái.");
                return;
            }
            await ReloadAsync();
            ToastService.ShowSuccess($"Đã {actionText} {result.UpdatedCount:N0}/{result.TargetRowCount:N0} dòng tổng hợp phụ cấp.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested) throw;
        }
        catch(Exception ex) { ToastService.ShowError(ex.Message); }
        finally { IsTogglingLock = false; CurrentActionLoadingText = null; }
    }
}
