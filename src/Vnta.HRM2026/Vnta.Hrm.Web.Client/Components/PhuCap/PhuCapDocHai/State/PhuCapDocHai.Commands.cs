using DevExpress.Blazor;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHai</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHai
{
    #region Command tính lại, làm mới và khóa

    /// <summary>Tính lại toàn kỳ đang áp dụng; command server giữ nguyên mọi dòng đã khóa.</summary>
    private async Task RecalculateAsync()
    {
        if(!CanRecalculate)
        {
            return;
        }

        try
        {
            IsRecalculating = true;
            BeginBusyState($"Đang tính lại phụ cấp độc hại kỳ {AppliedPeriodLabel}...");
            var result = await ExecuteDataOperationAsync(
                cancellationToken => DataProvider.RefreshAsync(AppliedMonth, AppliedYear, cancellationToken: cancellationToken));
            await ReloadAsync();
            ShowRecalculateSuccessToast(result);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(InvalidOperationException ex)
        {
            ShowOperationFailure(ex, "tính lại");
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể tính lại phụ cấp độc hại cho kỳ {PayrollMonth}/{PayrollYear}.", AppliedMonth, AppliedYear);
            ShowOperationFailure(ex, "tính lại");
        }
        finally
        {
            EndBusyState();
        }
    }

    /// <summary>Tính lại đúng một dòng sau confirm vì thao tác có thể thay thế điều chỉnh tay.</summary>
    private async Task RefreshRowAsync(HazardAllowanceListItemDto record)
    {
        if(!CanRefreshRow(record))
        {
            return;
        }

        var confirmed = await DialogService.ConfirmAsync(
            $"Sẽ tính lại phụ cấp độc hại của {GetEmployeeDisplay(record)} từ dữ liệu bảng công kỳ {AppliedPeriodLabel}. Giá trị điều chỉnh tay của dòng này sẽ được thay thế; dòng đã khóa không thể làm mới.",
            title: "Làm mới phụ cấp độc hại", okText: "Làm mới", cancelText: "Hủy", renderStyle: MessageBoxRenderStyle.Primary);
        if(!confirmed)
        {
            return;
        }

        try
        {
            IsRefreshingRow = true;
            LoadingText = $"Đang làm mới phụ cấp độc hại của {GetEmployeeDisplay(record)}...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();
            var result = await ExecuteDataOperationAsync(
                cancellationToken => DataProvider.RefreshAsync(
                    record.PayrollMonth,
                    record.PayrollYear,
                    record.PayrollAllowanceSummaryRecordId,
                    cancellationToken));
            await ReloadAsync();
            ToastService.ShowSuccess(result.SkippedLockedCount > 0
                ? "Dòng phụ cấp độc hại đã khóa nên không được làm mới."
                : result.UpdatedCount > 0 || result.CreatedCount > 0
                    ? $"Đã làm mới phụ cấp độc hại của {GetEmployeeDisplay(record)}."
                    : $"Dữ liệu phụ cấp độc hại của {GetEmployeeDisplay(record)} không thay đổi.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(InvalidOperationException ex)
        {
            ShowOperationFailure(ex, "làm mới");
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể làm mới phụ cấp độc hại cho kỳ {PayrollMonth}/{PayrollYear}.", record.PayrollMonth, record.PayrollYear);
            ShowOperationFailure(ex, "làm mới");
        }
        finally
        {
            IsRefreshingRow = false;
            LoadingText = DefaultLoadingText;
        }
    }

    /// <summary>Đảo trạng thái khóa của một summary row và reload server snapshot sau command.</summary>
    private async Task ToggleLockStateAsync(HazardAllowanceListItemDto record)
    {
        if(!CanToggleLock(record))
        {
            return;
        }

        var shouldLock = !record.IsLocked;
        try
        {
            BeginBusyState(shouldLock
                ? $"Đang khóa phụ cấp độc hại của {GetEmployeeDisplay(record)}..."
                : $"Đang mở khóa phụ cấp độc hại của {GetEmployeeDisplay(record)}...");
            await ExecuteDataOperationAsync(
                cancellationToken => DataProvider.SetLockStateAsync(
                    [record.PayrollAllowanceSummaryRecordId],
                    shouldLock,
                    cancellationToken));
            if(shouldLock && IsEditPopupVisible && EditModel.PayrollAllowanceSummaryRecordId == record.PayrollAllowanceSummaryRecordId)
            {
                IsEditPopupVisible = false;
            }

            await ReloadAsync();
            ToastService.ShowSuccess(shouldLock
                ? $"Đã khóa dòng phụ cấp độc hại của {GetEmployeeDisplay(record)}."
                : $"Đã mở khóa dòng phụ cấp độc hại của {GetEmployeeDisplay(record)}.");
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
            Logger.LogError(ex, "Không thể cập nhật trạng thái khóa phụ cấp độc hại cho kỳ {PayrollMonth}/{PayrollYear}.", record.PayrollMonth, record.PayrollYear);
            ShowOperationFailure(ex, shouldLock ? "khóa" : "mở khóa");
        }
        finally
        {
            EndBusyState();
        }
    }

    #endregion
}
