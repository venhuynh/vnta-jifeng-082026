using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Coordinator use cases for recalculation and lock-state commands.</summary>
public partial class PhuCapThamNien
{
    #region Tính lại phụ cấp

    /// <summary>Mở hộp xác nhận tính lại phụ cấp khi dữ liệu hiện tại cho phép thao tác.</summary>
    private Task OnRecalculateClickAsync()
    {
        if(!CanRecalculate)
        {
            return Task.CompletedTask;
        }

        IsRecalculateConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    /// <summary>Mở hộp chọn phạm vi để khóa dữ liệu phụ cấp.</summary>
    private Task OnLockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: true);

    /// <summary>Mở hộp chọn phạm vi để mở khóa dữ liệu phụ cấp.</summary>
    private Task OnUnlockSelectedAsync() => OpenLockActionPopupAsync(shouldLock: false);

    /// <summary>Đóng hộp xác nhận tính lại mà không thay đổi dữ liệu.</summary>
    private void CloseRecalculateConfirmPopup()
    {
        IsRecalculateConfirmPopupVisible = false;
    }

    #endregion

    #region Khóa và mở khóa dữ liệu

    /// <summary>Khởi tạo ngữ cảnh và mở hộp xác nhận khóa hoặc mở khóa.</summary>
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

    /// <summary>Đóng hộp xác nhận khóa/mở khóa và khôi phục trạng thái mặc định.</summary>
    private void CloseLockActionPopup()
    {
        if(IsRefreshing)
        {
            return;
        }

        IsLockActionPopupVisible = false;
    }

    /// <summary>Cập nhật phạm vi thao tác khóa/mở khóa khi phạm vi được hỗ trợ.</summary>
    private void SelectLockActionScope(string scope)
    {
        if(IsRefreshing)
        {
            return;
        }

        if(string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal))
        {
            if(!CanChooseSelectedRowsScope)
            {
                return;
            }

            SelectedLockActionScope = LockScopeSelectedRows;
            return;
        }

        if(string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }
    }

    /// <summary>Tính lại dữ liệu phụ cấp của kỳ hiện tại và tải lại lưới khi thành công.</summary>
    private async Task ConfirmRecalculateAsync()
    {
        CloseRecalculateConfirmPopup();

        if(!CanRecalculate)
        {
            return;
        }

        try
        {
            // Bộ lọc có thể đã đổi sau lần tải gần nhất; chỉ kỳ đã áp dụng mới xác định snapshot được phép tính lại.
            IsRefreshing = true;
            SetLoadingText($"Đang tính lại dữ liệu phụ cấp thâm niên kỳ {AppliedPeriodLabel}...");
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            await ClearSelectionAsync();

            var result = await DataProvider.RefreshAsync(
                new RefreshPayrollEmployeeSeniorityAllowanceRequest(
                    AppliedYear,
                    AppliedMonth),
                disposalTokenSource.Token);

            await ReloadAsync();
            ToastService.ShowSuccess(
                $"Đã tính lại {result.UpdatedCount:N0} dòng phụ cấp thâm niên của kỳ {AppliedPeriodLabel}, bỏ qua {result.SkippedLockedCount:N0} dòng đã khóa.");
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch(HrmApiException exception)
        {
            Logger.LogWarning(
                exception,
                "Không thể tính lại phụ cấp thâm niên kỳ {PayrollMonth:D2}/{PayrollYear}. TraceId: {TraceId}",
                AppliedMonth,
                AppliedYear,
                exception.TraceId);
            ToastService.ShowError(exception.UserMessage);
        }
        catch(UnauthorizedAccessException exception)
        {
            Logger.LogWarning(
                exception,
                "Người dùng không có quyền tính lại phụ cấp thâm niên kỳ {PayrollMonth:D2}/{PayrollYear}",
                AppliedMonth,
                AppliedYear);
            ToastService.ShowError("Bạn không có quyền tính lại phụ cấp thâm niên.");
        }
        catch(Exception exception)
        {
            Logger.LogError(
                exception,
                "Không thể tính lại phụ cấp thâm niên kỳ {PayrollMonth:D2}/{PayrollYear}",
                AppliedMonth,
                AppliedYear);
            ToastService.ShowError($"Không thể tính lại phụ cấp thâm niên của kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            IsRefreshing = false;
            SetLoadingText(DefaultLoadingText);
        }
    }

    #endregion

}

