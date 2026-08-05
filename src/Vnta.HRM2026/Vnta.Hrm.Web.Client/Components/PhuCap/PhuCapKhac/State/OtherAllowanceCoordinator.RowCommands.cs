using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;
using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

public sealed partial class OtherAllowanceCoordinator
{
    private bool CanEditRow(OtherAllowanceListItemDto row) => CanOperateOnCurrentDataset && row.Id != Guid.Empty && !row.IsLocked;
    private bool CanToggleLockRow(OtherAllowanceListItemDto row) => CanOperateOnCurrentDataset && row.Id != Guid.Empty;
    private async Task ToggleLockStateAsync(OtherAllowanceListItemDto row)
    {
        if(!CanToggleLockRow(row)) return;
        var targetIsLocked = !row.IsLocked;
        try
        {
            IsChangingLockState = true;
            LoadingText = targetIsLocked ? $"Đang khóa phụ cấp khác của {GetEmployeeDisplay(row)}..." : $"Đang mở khóa phụ cấp khác của {GetEmployeeDisplay(row)}...";
            await LockDataProvider.SetLockStateAsync(row.Id, targetIsLocked, row.UpdatedAtUtc ?? row.CreatedAtUtc, disposalTokenSource.Token);
            await LoadAsync();
            ToastService.ShowSuccess(targetIsLocked ? $"Đã khóa dòng phụ cấp khác của {GetEmployeeDisplay(row)}." : $"Đã mở khóa dòng phụ cấp khác của {GetEmployeeDisplay(row)}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(OtherAllowanceConflictException) { ToastService.ShowWarning("Dữ liệu đã được thay đổi bởi thao tác khác. Vui lòng tải lại trước khi thử lại."); }
        catch(HrmApiException exception) when(exception.Kind == HrmApiErrorKind.Conflict) { await ReloadAfterConflictAsync(exception.UserMessage); }
        catch(UnauthorizedAccessException) { ToastService.ShowWarning("Bạn không có quyền khóa hoặc mở khóa phụ cấp khác."); }
        catch(HrmApiException exception) when(exception.Kind is HrmApiErrorKind.Unauthenticated or HrmApiErrorKind.Forbidden) { ToastService.ShowWarning("Bạn không có quyền khóa hoặc mở khóa phụ cấp khác."); }
        catch(InvalidOperationException exception) { ToastService.ShowWarning(exception.Message); }
        catch(Exception exception) { Logger.LogError(exception, "Không thể thay đổi trạng thái khóa phụ cấp khác của {EmployeeDisplay}.", GetEmployeeDisplay(row)); ToastService.ShowError("Không thể thay đổi trạng thái khóa phụ cấp khác. Vui lòng thử lại."); }
        finally { if(!disposalTokenSource.IsCancellationRequested) { IsChangingLockState = false; LoadingText = DefaultLoadingText; } }
    }

    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if(!CanOperateOnCurrentDataset) return Task.CompletedTask;

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
        if(IsChangingLockState) return;
        IsLockActionPopupVisible = false;
        SelectedLockActionScope = LockScopeSelectedRows;
    }

    private void SelectLockActionScopeCore(string scope)
    {
        if(IsChangingLockState) return;
        if(string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal) && CanChooseSelectedRowsScope)
        {
            SelectedLockActionScope = LockScopeSelectedRows;
        }
        else if(IsWholePeriodLockStateScope(scope))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }
    }

    private async Task ConfirmLockActionCoreAsync()
    {
        if(!CanConfirmLockAction || disposalTokenSource.IsCancellationRequested) return;

        var shouldLock = PendingLockActionState;
        var scope = SelectedLockActionScope;
        var payrollMonth = PendingLockActionMonth;
        var payrollYear = PendingLockActionYear;
        var selectedRows = IsWholePeriodLockStateScope(scope)
            ? null
            : GetSelectedRows().ToArray();
        if(selectedRows is { Length: 0 })
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
            return;
        }

        var actionText = shouldLock ? "khóa" : "mở khóa";
        try
        {
            IsChangingLockState = true;
            IsLockActionPopupVisible = false;
            LoadingText = IsWholePeriodLockStateScope(scope)
                ? $"Đang {actionText} phụ cấp khác kỳ {payrollMonth:00}/{payrollYear}..."
                : $"Đang {actionText} {selectedRows!.Length:N0} dòng phụ cấp khác đã chọn...";

            var result = await LockDataProvider.SetLockStateBatchAsync(
                payrollMonth,
                payrollYear,
                shouldLock,
                selectedRows,
                disposalTokenSource.Token);
            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu phụ cấp khác của kỳ {payrollMonth:00}/{payrollYear} để {actionText}.");
                return;
            }

            if(result.UpdatedCount == 0)
            {
                ToastService.ShowInfo(BuildNoEligibleRowsMessage(shouldLock, result));
                return;
            }

            selectedItems = [];
            await LoadAsync();
            ToastService.ShowSuccess(BuildLockActionSuccessMessage(shouldLock, result, payrollMonth, payrollYear));
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(OtherAllowanceConflictException)
        {
            await ReloadAfterConflictAsync("Dữ liệu đã được thay đổi bởi thao tác khác. Vui lòng tải lại trước khi thử lại.");
        }
        catch(HrmApiException exception) when(exception.Kind == HrmApiErrorKind.Conflict)
        {
            await ReloadAfterConflictAsync(exception.UserMessage);
        }
        catch(UnauthorizedAccessException)
        {
            ToastService.ShowWarning($"Bạn không có quyền {actionText} phụ cấp khác.");
        }
        catch(HrmApiException exception) when(exception.Kind is HrmApiErrorKind.Unauthenticated or HrmApiErrorKind.Forbidden)
        {
            ToastService.ShowWarning($"Bạn không có quyền {actionText} phụ cấp khác.");
        }
        catch(InvalidOperationException exception) { ToastService.ShowWarning(exception.Message); }
        catch(Exception exception)
        {
            Logger.LogError(exception, "Không thể {ActionText} phụ cấp khác kỳ {PayrollMonth}/{PayrollYear}.", actionText, payrollMonth, payrollYear);
            ToastService.ShowError($"Không thể {actionText} phụ cấp khác. Vui lòng thử lại.");
        }
        finally
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                IsChangingLockState = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private static string BuildLockActionSuccessMessage(
        bool shouldLock,
        SetOtherAllowanceBatchLockStateResult result,
        int payrollMonth,
        int payrollYear)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var scopeText = result.IsWholePeriod ? $" của kỳ {payrollMonth:00}/{payrollYear}" : " đã chọn";
        var details = new List<string>();
        if(result.UnchangedCount > 0) details.Add($"giữ nguyên {result.UnchangedCount:N0} dòng đã đúng trạng thái");
        if(result.SkippedSummaryLockedCount > 0) details.Add($"bỏ qua {result.SkippedSummaryLockedCount:N0} dòng có summary đã khóa");
        return $"Đã {actionText} {result.UpdatedCount:N0}/{result.TargetRowCount:N0} dòng phụ cấp khác{scopeText}"
            + (details.Count == 0 ? "." : $", {string.Join(", ", details)}.");
    }

    private static string BuildNoEligibleRowsMessage(bool shouldLock, SetOtherAllowanceBatchLockStateResult result)
    {
        var actionText = shouldLock ? "khóa" : "mở khóa";
        return result.SkippedSummaryLockedCount > 0
            ? $"Không có dòng nào được {actionText}; {result.UnchangedCount:N0} dòng đã đúng trạng thái và {result.SkippedSummaryLockedCount:N0} dòng bị summary đã khóa bảo vệ."
            : $"Không có dòng nào cần {actionText}; {result.UnchangedCount:N0} dòng đã ở trạng thái phù hợp.";
    }

    private async Task ReloadAfterConflictAsync(string message)
    {
        ToastService.ShowWarning(message);
        if(!disposalTokenSource.IsCancellationRequested) await LoadAsync();
    }
}
