namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

public sealed partial class OtherResponsibilityAllowanceCoordinator
{
    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if (!CanUseAppliedPeriodActions) return Task.CompletedTask;
        PendingLockActionState = shouldLock;
        PendingLockActionMonth = AppliedMonth;
        PendingLockActionYear = AppliedYear;
        SelectedLockActionScope = CanChooseSelectedRowsScope ? LockScopeSelectedRows : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseLockActionPopup()
    {
        if (!IsRunningScreenAction) IsLockActionPopupVisible = false;
    }

    private void SelectLockActionScope(string scope)
    {
        if (IsRunningScreenAction) return;
        if (scope == LockScopeSelectedRows && CanChooseSelectedRowsScope) SelectedLockActionScope = scope;
        if (scope == LockScopeWholePeriod) SelectedLockActionScope = scope;
    }

    private async Task ConfirmLockActionCoreAsync()
    {
        if (!CanConfirmLockAction) return;
        var selectedRecords = SelectedLockActionScope == LockScopeSelectedRows
            ? GetSelectedRecords().Where(record => record.PayrollAllowanceSummaryRecordId != Guid.Empty).DistinctBy(record => record.PayrollAllowanceSummaryRecordId).ToArray()
            : null;
        if (SelectedLockActionScope == LockScopeSelectedRows && selectedRecords!.Length == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một dòng hợp lệ hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
            return;
        }

        var shouldLock = PendingLockActionState;
        try
        {
            SetOtherResponsibilityAllowanceBatchLockStateResult? result = null;
            await RunScreenActionAsync(shouldLock ? "Đang khóa dữ liệu phụ cấp trách nhiệm khác..." : "Đang mở khóa dữ liệu phụ cấp trách nhiệm khác...", async () =>
            {
                result = await DataProvider.SetLockStateBatchAsync(PendingLockActionYear, PendingLockActionMonth, shouldLock, selectedRecords, disposalTokenSource.Token);
                await ClearGridSelectionAsync();
                await ReloadAsync();
            });
            if (HasLoadError || result is null) return;
            IsLockActionPopupVisible = false;
            ToastService.ShowSuccess($"Đã {(shouldLock ? "khóa" : "mở khóa")} {result.UpdatedCount:N0}/{result.TargetRowCount:N0} dòng phụ cấp trách nhiệm khác.");
        }
        catch (OperationCanceledException) when (IsDisposalRequested) { }
        catch (InvalidOperationException)
        {
            ToastService.ShowWarning(shouldLock ? "Không thể khóa dữ liệu đã chọn. Hãy tải lại dữ liệu và thử lại." : "Không thể mở khóa dữ liệu đã chọn. Hãy tải lại dữ liệu và thử lại.");
        }
        catch (Exception)
        {
            ToastService.ShowError(shouldLock ? "Không thể khóa phụ cấp trách nhiệm khác." : "Không thể mở khóa phụ cấp trách nhiệm khác.");
        }
    }
}
