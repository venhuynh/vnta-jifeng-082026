using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan;

public partial class PhuCapChuyenCan
{
    /// <summary>Chuyển đổi trạng thái cho luồng <c>ToggleLockStateAsync</c>.</summary>
    private async Task ToggleLockStateAsync(AttendanceAllowanceResultRecord record)
    {
        if(!CanToggleLockState(record))
        {
            return;
        }

        var shouldLock = !record.IsLocked;
        try
        {
            IsRefreshing = true;
            CurrentLoadingText = shouldLock
                ? $"Đang khóa phụ cấp chuyên cần của {record.EmployeeDisplay}..."
                : $"Đang mở khóa phụ cấp chuyên cần của {record.EmployeeDisplay}...";
            await InvokeAsync(StateHasChanged);

            var result = await LockDataProvider.SetLockStateForRowsAsync(
                AppliedYear,
                AppliedMonth,
                shouldLock,
                [new AttendanceAllowanceLockItem(record.Id, record.UpdatedAtUtc)],
                disposalTokenSource.Token);

            if(result.UpdatedCount > 0)
            {
                await ReloadAsync();
                ToastService.ShowSuccess(shouldLock
                    ? $"Đã khóa dòng phụ cấp chuyên cần của {record.EmployeeDisplay}."
                    : $"Đã mở khóa dòng phụ cấp chuyên cần của {record.EmployeeDisplay}.");
                return;
            }

            ToastService.ShowInfo("Dòng phụ cấp chuyên cần đã ở trạng thái phù hợp hoặc kỳ lương đã khóa.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy nên không phát feedback muộn.
        }
        catch(InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch(Exception)
        {
            ToastService.ShowError(shouldLock
                ? "Không thể khóa dòng phụ cấp chuyên cần."
                : "Không thể mở khóa dòng phụ cấp chuyên cần.");
        }
        finally
        {
            IsRefreshing = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
        }
    }

}

