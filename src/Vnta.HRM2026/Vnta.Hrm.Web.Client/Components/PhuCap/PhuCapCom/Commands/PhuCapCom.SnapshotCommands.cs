using Vnta.Hrm.Web.Client.Services.Ui;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Owns snapshot refresh and row lock commands for the meal-allowance screen.</summary>
public partial class PhuCapCom
{
    private async Task ToggleLockStateAsync(MealAllowanceRecord record)
    {
        if(!CanToggleLock(record))
        {
            return;
        }

        var shouldLock = !record.IsLocked;
        var (month, year) = GetAppliedPayrollPeriod();
        try
        {
            IsRefreshing = true;
            LoadingText = $"Đang {(shouldLock ? "khóa" : "mở khóa")} phụ cấp cơm của {record.EmployeeDisplay}...";
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await DataProvider.SetLockStateBatchAsync(
                new SetMealAllowanceLockStateBatchRequest(
                    year,
                    month,
                    shouldLock,
                    MealAllowanceLockActionScope.SelectedRows,
                    [record.Id]),
                disposalTokenSource.Token);

            if(result.TargetRowCount == 0)
            {
                ToastService.ShowInfo("Dòng phụ cấp cơm không còn tồn tại trong kỳ lương đang áp dụng.");
                return;
            }

            await ReloadAsync();
            ToastService.ShowSuccess(result.UpdatedCount == 0
                ? $"Dòng phụ cấp cơm của {record.EmployeeDisplay} đã ở trạng thái mong muốn."
                : shouldLock
                    ? $"Đã khóa dòng phụ cấp cơm của {record.EmployeeDisplay}."
                    : $"Đã mở khóa dòng phụ cấp cơm của {record.EmployeeDisplay}.");
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
            ToastService.ShowError($"Không thể cập nhật trạng thái khóa của {record.EmployeeDisplay}.");
        }
        finally
        {
            IsRefreshing = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    /// <summary>Đóng cho luồng <c>CloseRecalculateConfirmPopup</c>.</summary>
    private void CloseRecalculateConfirmPopup()
    {
        if(!IsRefreshing)
        {
            IsRecalculateConfirmPopupVisible = false;
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnRecalculatePopupVisibleChanged</c>.</summary>
    private Task OnRecalculatePopupVisibleChanged(bool visible)
    {
        if(!visible)
        {
            CloseRecalculateConfirmPopup();
        }

        return Task.CompletedTask;
    }

    /// <summary>Xác nhận cho luồng <c>ConfirmRecalculateAsync</c>.</summary>
    private async Task ConfirmRecalculateAsync()
    {
        CloseRecalculateConfirmPopup();

        if(!CanRefreshSnapshot)
        {
            return;
        }

        var (targetPayrollMonth, targetPayrollYear) = GetAppliedPayrollPeriod();
        var targetPayrollPeriod = FormatPayrollPeriod(targetPayrollMonth, targetPayrollYear);

        try
        {
            LoadErrorMessage = null;
            LoadingText = $"Đang tính lại dữ liệu phụ cấp cơm kỳ {targetPayrollPeriod}...";
            IsRefreshing = true;

            var result = await DataProvider.RefreshAsync(
                targetPayrollMonth,
                targetPayrollYear,
                cancellationToken: disposalTokenSource.Token);

            HasRequestedData = true;
            await ReloadAsync();
            ShowRefreshResultToast(result);
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
            ToastService.ShowError("Không thể tính lại dữ liệu phụ cấp cơm từ dữ liệu công.");
        }
        finally
        {
            IsRefreshing = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

    /// <summary>Làm mới cho luồng <c>RefreshRowAsync</c>.</summary>
    private async Task RefreshRowAsync(MealAllowanceRecord record)
    {
        if(!CanRefreshRow(record))
        {
            return;
        }

        var (month, year) = GetAppliedPayrollPeriod();
        try
        {
            IsRefreshing = true;
            LoadingText = $"Đang làm mới phụ cấp cơm của {record.EmployeeDisplay}...";
            var result = await DataProvider.RefreshAsync(month, year, record.EmployeeId, disposalTokenSource.Token);
            await ReloadAsync();
            ShowRefreshResultToast(result);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể làm mới phụ cấp cơm của {record.EmployeeDisplay}.");
        }
        finally
        {
            IsRefreshing = false;
            LoadingText = HrmUiDefaults.LoadingText;
        }
    }

}
