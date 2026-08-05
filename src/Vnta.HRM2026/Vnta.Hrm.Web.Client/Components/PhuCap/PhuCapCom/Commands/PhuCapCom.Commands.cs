using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapCom;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Owns selection-sensitive lock commands and their confirmation lifecycle.</summary>
public partial class PhuCapCom
{
    private Task OnRecalculateClick()
    {
        if(!CanRefreshSnapshot)
        {
            return Task.CompletedTask;
        }

        IsRecalculateConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private Task OnRecalculateClickAsync() => OnRecalculateClick();

    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(true);

    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(false);

    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if(!CanOperateOnCurrentDataset)
        {
            return Task.CompletedTask;
        }

        var (month, year) = GetAppliedPayrollPeriod();
        PendingLockActionState = shouldLock;
        PendingLockActionMonth = month;
        PendingLockActionYear = year;
        SelectedLockActionScope = CanChooseSelectedRowsScope
            ? LockScopeSelectedRows
            : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseLockActionPopup()
    {
        if(!IsRefreshing)
        {
            IsLockActionPopupVisible = false;
        }
    }

    private Task OnLockActionPopupVisibleChanged(bool visible)
    {
        if(!visible)
        {
            CloseLockActionPopup();
        }

        return Task.CompletedTask;
    }

    private Task SelectLockActionScope(string scope)
    {
        if(IsRefreshing)
        {
            return Task.CompletedTask;
        }

        if(string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal)
           || (string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal) && CanChooseSelectedRowsScope))
        {
            SelectedLockActionScope = scope;
        }

        return Task.CompletedTask;
    }

    private async Task ConfirmLockActionAsync()
    {
        var shouldLock = PendingLockActionState;
        var actionScope = SelectedLockActionScope;
        if(!CanOperateOnCurrentDataset)
        {
            return;
        }

        Guid[]? targetRecordIds = null;
        var targetRowCount = 0;
        if(!IsWholePeriodLockActionScope(actionScope))
        {
            var selectedRecords = GetSelectedRecords().DistinctBy(record => record.Id).ToArray();
            if(selectedRecords.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }

            targetRecordIds = selectedRecords.Select(record => record.Id).ToArray();
            targetRowCount = targetRecordIds.Length;
        }

        try
        {
            IsRefreshing = true;
            IsLockActionPopupVisible = false;
            LoadingText = BuildLockActionPendingLoadingMessage(shouldLock, actionScope, targetRowCount);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await DataProvider.SetLockStateBatchAsync(
                new SetMealAllowanceLockStateBatchRequest(
                    PendingLockActionYear,
                    PendingLockActionMonth,
                    shouldLock,
                    IsWholePeriodLockActionScope(actionScope)
                        ? MealAllowanceLockActionScope.WholePeriod
                        : MealAllowanceLockActionScope.SelectedRows,
                    targetRecordIds),
                disposalTokenSource.Token);

            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo(BuildLockActionNoDataMessage(shouldLock, actionScope));
                return;
            }

            if(result.UpdatedCount == 0)
            {
                ToastService.ShowInfo(BuildLockActionAlreadyAppliedMessage(shouldLock, actionScope, result.TargetRowCount));
                return;
            }

            await ClearSelectionAsync();
            await ReloadAsync();
            ToastService.ShowSuccess(BuildLockActionSuccessMessage(shouldLock, actionScope, result.TargetRowCount, result.UpdatedCount));
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể {(shouldLock ? "khóa" : "mở khóa")} dữ liệu phụ cấp cơm của kỳ {PendingLockActionPeriodLabel}.");
        }
        finally
        {
            IsRefreshing = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }
}
