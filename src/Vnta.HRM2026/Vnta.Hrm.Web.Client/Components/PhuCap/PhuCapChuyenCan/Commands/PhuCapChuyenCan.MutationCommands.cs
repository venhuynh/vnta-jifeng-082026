using Microsoft.AspNetCore.Components.Forms;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Models;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan;

public partial class PhuCapChuyenCan
{
    /// <summary>Xử lý sự kiện cho luồng <c>OnRecalculateClickAsync</c>.</summary>
    private Task OnRecalculateClickAsync()
    {
        if(!CanRecalculate)
        {
            return Task.CompletedTask;
        }

        IsRecalculateConfirmPopupVisible = true;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnLockSelectedAsync</c>.</summary>
    private Task OnLockSelectedAsync() => OnLockSelectedResultsClick();

    /// <summary>Xử lý sự kiện cho luồng <c>OnUnlockSelectedAsync</c>.</summary>
    private Task OnUnlockSelectedAsync() => OnUnlockSelectedResultsClick();

    /// <summary>Đóng cho luồng <c>CloseRecalculateConfirmPopup</c>.</summary>
    private void CloseRecalculateConfirmPopup()
    {
        if(IsConfirmationBusy)
        {
            return;
        }

        IsRecalculateConfirmPopupVisible = false;
    }

    /// <summary>Xác nhận cho luồng <c>ConfirmRecalculateAsync</c>.</summary>
    private async Task ConfirmRecalculateAsync()
    {
        if(IsConfirmationBusy)
        {
            return;
        }

        IsConfirmationBusy = true;
        try
        {
            await ExecuteRecalculateAsync();
        }
        finally
        {
            IsConfirmationBusy = false;
            IsRecalculateConfirmPopupVisible = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Thực hiện xử lý cho luồng <c>ExecuteRecalculateAsync</c>.</summary>
    private async Task ExecuteRecalculateAsync()
    {
        if(!CanRecalculate)
        {
            return;
        }

        var payrollPeriod = AppliedPeriodLabel;
        try
        {
            IsRefreshing = true;
            CurrentLoadingText = $"Đang tính lại dữ liệu phụ cấp chuyên cần kỳ {payrollPeriod}...";
            var result = await RefreshDataProvider.RefreshAsync(
                AppliedMonth,
                AppliedYear,
                disposalTokenSource.Token);

            await ReloadAsync();
            ShowRefreshResultToast(result);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy.
        }
        catch(InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể tính lại dữ liệu phụ cấp chuyên cần.");
        }
        finally
        {
            IsRefreshing = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnLockSelectedResultsClick</c>.</summary>
    private Task OnLockSelectedResultsClick() => OpenLockActionPopupAsync(shouldLock: true);

    /// <summary>Xử lý sự kiện cho luồng <c>OnUnlockSelectedResultsClick</c>.</summary>
    private Task OnUnlockSelectedResultsClick() => OpenLockActionPopupAsync(shouldLock: false);

    /// <summary>Mở cho luồng <c>OpenEditPopup</c>.</summary>
    private void OpenEditPopup(AttendanceAllowanceResultRecord record)
    {
        if(!CanEditRow(record))
        {
            return;
        }

        EditModel = new PhuCapChuyenCanEditModel
        {
            Id = record.Id,
            EmployeeDisplay = record.EmployeeDisplay,
            PayrollPeriodDisplay = record.PayrollPeriodDisplay,
            ActualWorkdayCount = record.ActualWorkdayCount,
            OriginalActualWorkdayCount = record.ActualWorkdayCount,
            StandardWorkdayCount = record.StandardWorkdayCount,
            OriginalStandardWorkdayCount = record.StandardWorkdayCount,
            IsLocked = record.IsLocked,
            OriginalUpdatedAtUtc = record.UpdatedAtUtc
        };
        EditContext = new EditContext(EditModel);
        IsEditPopupVisible = true;
    }

    /// <summary>Đóng cho luồng <c>CloseEditPopup</c>.</summary>
    private void CloseEditPopup()
    {
        if(IsSavingEdit)
        {
            return;
        }

        CloseEditPopupCore();
    }

    /// <summary>Lưu cho luồng <c>SaveEditAsync</c>.</summary>
    private async Task SaveEditAsync()
    {
        if(!CanSaveEdit)
        {
            return;
        }

        if(!EditContext.Validate())
        {
            return;
        }

        try
        {
            IsSavingEdit = true;
            CurrentLoadingText = $"Đang điều chỉnh ngày công của {EditModel.EmployeeDisplay}...";
            await InvokeAsync(StateHasChanged);

            await WorkdayAdjustmentDataProvider.UpdateWorkdaysAsync(
                EditModel.Id,
                EditModel.ActualWorkdayCount,
                EditModel.StandardWorkdayCount,
                EditModel.OriginalUpdatedAtUtc,
                disposalTokenSource.Token);

            CloseEditPopupCore();
            await ReloadAsync();
            ToastService.ShowSuccess("Đã điều chỉnh ngày công phụ cấp chuyên cần.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy nên không được cập nhật lại view-state.
        }
        catch(InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể điều chỉnh số ngày công thực tế. Vui lòng kiểm tra dữ liệu và thử lại.");
        }
        finally
        {
            IsSavingEdit = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    /// <summary>Đóng cho luồng <c>CloseEditPopupCore</c>.</summary>
    private void CloseEditPopupCore()
    {
        IsEditPopupVisible = false;
        EditModel = new();
        EditContext = new EditContext(EditModel);
    }

    /// <summary>Đóng cho luồng <c>CloseLockActionPopup</c>.</summary>
    private void CloseLockActionPopup()
    {
        if(IsRefreshing)
        {
            return;
        }

        IsLockActionPopupVisible = false;
        SelectedLockActionScope = LockScopeSelectedRows;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnLockActionPopupVisibleChanged</c>.</summary>
    private Task OnLockActionPopupVisibleChanged(bool visible)
    {
        if(!visible)
        {
            CloseLockActionPopup();
        }

        return Task.CompletedTask;
    }

    /// <summary>Xác nhận cho luồng <c>ConfirmLockActionAsync</c>.</summary>
    private async Task ConfirmLockActionAsync()
    {
        var shouldLock = PendingLockActionState;
        var scope = SelectedLockActionScope;
        var payrollMonth = PendingLockActionMonth;
        var payrollYear = PendingLockActionYear;
        if(!CanOperateOnCurrentDataset)
        {
            return;
        }

        AttendanceAllowanceResultRecord[]? targetRecords = null;
        if(!IsWholePeriodLockStateScope(scope))
        {
            var selectedRecords = GetSelectedResults()
                .Where(result => result.IsLocked != shouldLock)
                .DistinctBy(result => result.Id)
                .ToArray();
            if(selectedRecords.Length == 0)
            {
                ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
                return;
            }

            targetRecords = selectedRecords;
        }

        try
        {
            IsRefreshing = true;
            IsLockActionPopupVisible = false;
            CurrentLoadingText = GetLockStateLoadingMessage(shouldLock);
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = IsWholePeriodLockStateScope(scope)
                ? await LockDataProvider.SetLockStateForWholePeriodAsync(
                    payrollYear,
                    payrollMonth,
                    shouldLock,
                    disposalTokenSource.Token)
                : await LockDataProvider.SetLockStateForRowsAsync(
                    payrollYear,
                    payrollMonth,
                    shouldLock,
                    targetRecords!
                        .Select(record => new AttendanceAllowanceLockItem(record.Id, record.UpdatedAtUtc))
                        .ToArray(),
                    disposalTokenSource.Token);

            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo(BuildLockStateNoDataMessage(shouldLock, scope, payrollMonth, payrollYear));
                return;
            }

            if(result.UpdatedCount == 0)
            {
                ToastService.ShowInfo(BuildLockStateNoEligibleRowsMessage(shouldLock, scope, result));
                return;
            }

            await ClearSelectionAsync();
            await ReloadAsync();
            ToastService.ShowSuccess(BuildLockStateSuccessMessage(
                shouldLock,
                scope,
                result,
                payrollMonth,
                payrollYear));
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
            ToastService.ShowError(BuildLockStateFailureMessage(shouldLock));
        }
        finally
        {
            IsRefreshing = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

    /// <summary>Mở cho luồng <c>OpenLockActionPopupAsync</c>.</summary>
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

    /// <summary>Cập nhật lựa chọn cho luồng <c>SelectLockActionScope</c>.</summary>
    private void SelectLockActionScope(string scope)
    {
        if(IsRefreshing)
        {
            return;
        }

        if(string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal))
        {
            if(CanChooseSelectedRowsScope)
            {
                SelectedLockActionScope = LockScopeSelectedRows;
            }

            return;
        }

        if(string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }
    }
}
