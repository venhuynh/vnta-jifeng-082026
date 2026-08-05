namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Điều phối thao tác khóa/mở khóa theo phạm vi cho màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHai
{
    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);

    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if(!CanOperateOnCurrentDataset)
        {
            return Task.CompletedTask;
        }

        PendingLockActionState = shouldLock;
        PendingLockActionMonth = AppliedMonth;
        PendingLockActionYear = AppliedYear;
        SelectedLockActionScope = CanChooseSelectedRowsScope
            ? LockScopeSelectedRows
            : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseLockActionPopup()
    {
        if(!IsLockActionProcessing)
        {
            IsLockActionPopupVisible = false;
        }
    }

    private void SelectLockActionScope(string scope)
    {
        if(IsLockActionProcessing)
        {
            return;
        }

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
        if(!CanConfirmLockAction)
        {
            return;
        }

        var shouldLock = PendingLockActionState;
        var isWholePeriod = string.Equals(SelectedLockActionScope, LockScopeWholePeriod, StringComparison.Ordinal);
        Guid[]? targetRecordIds = null;
        var targetRowCount = 0;
        if(!isWholePeriod)
        {
            var selectedRecords = GetSelectedVisibleRecords();
            if(selectedRecords.Count == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }

            targetRecordIds = selectedRecords
                .Select(record => record.PayrollAllowanceSummaryRecordId)
                .ToArray();
            targetRowCount = targetRecordIds.Length;

            if(shouldLock
                && IsEditPopupVisible
                && targetRecordIds.Contains(EditModel.PayrollAllowanceSummaryRecordId))
            {
                IsEditPopupVisible = false;
            }
        }
        else if(shouldLock && IsEditPopupVisible)
        {
            IsEditPopupVisible = false;
        }

        try
        {
            IsLockActionProcessing = true;
            IsLockActionPopupVisible = false;
            BeginBusyState(BuildLockActionLoadingText(shouldLock, isWholePeriod, targetRowCount));
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await ExecuteDataOperationAsync(
                cancellationToken => DataProvider.SetLockStateBatchAsync(
                    PendingLockActionYear,
                    PendingLockActionMonth,
                    shouldLock,
                    targetRecordIds,
                    cancellationToken));

            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo("Không có dòng phụ cấp độc hại phù hợp để cập nhật trạng thái khóa.");
                return;
            }

            if(result.UpdatedCount == 0)
            {
                ToastService.ShowInfo(BuildLockActionAlreadyAppliedMessage(shouldLock, result.TargetRowCount));
                return;
            }

            await ReloadAsync();
            ToastService.ShowSuccess(BuildLockActionSuccessMessage(shouldLock, isWholePeriod, result.TargetRowCount, result.UpdatedCount));
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(InvalidOperationException ex)
        {
            ShowOperationFailure(ex, shouldLock ? "khóa" : "mở khóa");
        }
        catch(Exception ex)
        {
            Logger.LogError(
                ex,
                "Không thể {Action} phụ cấp độc hại cho kỳ {PayrollMonth}/{PayrollYear}.",
                shouldLock ? "khóa" : "mở khóa",
                PendingLockActionMonth,
                PendingLockActionYear);
            ShowOperationFailure(ex, shouldLock ? "khóa" : "mở khóa");
        }
        finally
        {
            IsLockActionProcessing = false;
            EndBusyState();
        }
    }

    private string BuildLockActionLoadingText(bool shouldLock, bool isWholePeriod, int selectedRowCount) =>
        isWholePeriod
            ? $"Đang {(shouldLock ? "khóa" : "mở khóa")} phụ cấp độc hại của toàn bộ kỳ {PendingLockActionPeriodLabel}..."
            : $"Đang {(shouldLock ? "khóa" : "mở khóa")} {selectedRowCount:N0} dòng phụ cấp độc hại đã chọn...";

    private static string BuildLockActionAlreadyAppliedMessage(bool shouldLock, int targetRowCount) =>
        $"{targetRowCount:N0} dòng phụ cấp độc hại đã {(shouldLock ? "ở trạng thái khóa" : "ở trạng thái mở khóa")}.";

    private static string BuildLockActionSuccessMessage(
        bool shouldLock,
        bool isWholePeriod,
        int targetRowCount,
        int updatedCount) =>
        $"Đã {(shouldLock ? "khóa" : "mở khóa")} {updatedCount:N0}/{targetRowCount:N0} dòng phụ cấp độc hại "
        + (isWholePeriod ? "của toàn bộ kỳ lương." : "đã chọn.");
}
