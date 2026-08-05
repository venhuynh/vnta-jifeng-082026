using System.Globalization;
using System.Net;
using System.Text;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using Vnta.Hrm.Web.Client.Components.Shared.Models;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe.Models;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapPhepLe;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLe
{
    #region Row Actions

    /// <summary>Tính lại và đồng bộ riêng một dòng phụ cấp Phép - Lễ chưa bị khóa.</summary>
    private async Task RefreshRowAsync(LeaveHolidayAllowanceRecord row)
    {
        if (!CanRefreshRow(row))
        {
            return;
        }

        try
        {
            await RunScreenActionAsync(
                $"Đang làm mới phụ cấp Phép - Lễ của {row.EmployeeDisplay}...",
                async () =>
                {
                    var result = await ExecuteDataOperationAsync(
                        token => DataProvider.RecalculateAsync(
                            row.PayrollMonth,
                            row.PayrollYear,
                            token,
                            row.Id),
                        disposalTokenSource.Token);

                    // Dòng có thể vừa bị khóa hoặc thay đổi từ thao tác đồng thời, nên luôn đọc lại snapshot.
                    await ReloadSnapshotAfterBatchActionAsync();
                    if (result.UpdatedCount > 0)
                    {
                        ToastService.ShowSuccess($"Đã làm mới phụ cấp Phép - Lễ của {row.EmployeeDisplay}.");
                    }
                    else if (result.SkippedLockedCount > 0)
                    {
                        ToastService.ShowWarning($"Dòng phụ cấp Phép - Lễ của {row.EmployeeDisplay} đã khóa nên không được làm mới.");
                    }
                    else
                    {
                        ToastService.ShowInfo($"Không tìm thấy dòng phụ cấp Phép - Lễ của {row.EmployeeDisplay} để làm mới.");
                    }
                });
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            ToastService.ShowError($"Không thể làm mới phụ cấp Phép - Lễ của {row.EmployeeDisplay}.");
        }
    }

    /// <summary>Chuyển đổi trạng thái cho luồng <c>ToggleLockStateAsync</c>.</summary>
    private async Task ToggleLockStateAsync(LeaveHolidayAllowanceRecord row)
    {
        if (!CanToggleLock(row))
        {
            return;
        }

        var nextLockedState = !row.IsLocked;

        try
        {
            await RunScreenActionAsync(
                row.IsLocked
                    ? $"Đang mở khóa dữ liệu của {row.EmployeeDisplay}..."
                    : $"Đang khóa dữ liệu của {row.EmployeeDisplay}...",
                async () =>
                {
                    var updatedRecord = await ExecuteDataOperationAsync(
                        token => DataProvider.SetLockStateAsync(
                            row.Id,
                            nextLockedState,
                            row.UpdatedAtUtc ?? row.CreatedAtUtc,
                            token),
                        disposalTokenSource.Token);

                    ReplaceUpdatedRecordInState(updatedRecord);
                    ToastService.ShowSuccess(
                        updatedRecord.IsLocked
                            ? $"Đã khóa dòng phụ cấp Phép - Lễ của {updatedRecord.EmployeeDisplay}."
                            : $"Đã mở khóa dòng phụ cấp Phép - Lễ của {updatedRecord.EmployeeDisplay}.");
                });
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception)
        {
            ToastService.ShowError($"Không thể cập nhật trạng thái khóa của {row.EmployeeDisplay}.");
        }
    }

    #endregion
}
