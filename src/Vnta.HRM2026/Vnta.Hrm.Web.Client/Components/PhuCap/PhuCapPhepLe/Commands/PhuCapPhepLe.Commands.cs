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
    #region Batch Screen Actions

    /// <summary>Xử lý sự kiện cho luồng <c>OnRecalculateClickAsync</c>.</summary>
    private async Task OnRecalculateClickAsync()
    {
        if (!CanRecalculate)
        {
            return;
        }

        var payrollPeriod = CurrentPayrollPeriodDisplay;
        var confirmed = await DialogService.ConfirmAsync(
            $"Sẽ tính lại Công HC từ các mã kết quả công được đánh dấu Phép - Lễ, áp lương cơ bản ngày và đồng bộ tổng tiền về Tổng hợp phụ cấp cho các dòng đang mở của kỳ {payrollPeriod}. Số ngày Lễ nhập tay được giữ nguyên; các dòng đã khóa sẽ được bỏ qua.",
            title: "Tính lại phụ cấp Phép - Lễ",
            okText: "Tính lại",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Primary);

        if (!confirmed)
        {
            return;
        }

        try
        {
            await RunScreenActionAsync(
                $"Đang tính lại dữ liệu phụ cấp Phép - Lễ kỳ {payrollPeriod}...",
                async () =>
                {
                    var result = await ExecuteDataOperationAsync(
                        token => DataProvider.RecalculateAsync(AppliedMonth, AppliedYear, token),
                        disposalTokenSource.Token);

                    if (result.UpdatedCount > 0)
                    {
                        await ReloadSnapshotAfterBatchActionAsync();
                    }

                    ShowRecalculateToast(result, payrollPeriod);
                });
        }
        catch (OperationCanceledException)
        {
            if (!disposalTokenSource.IsCancellationRequested)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Không thể tính lại dữ liệu phụ cấp Phép - Lễ. {ex.Message}");
        }
    }

    /// <summary>Mở cho luồng <c>OpenLockActionPopupAsync</c>.</summary>
    private Task OpenLockActionPopupAsync(bool shouldLock)
    {
        if (!CanOperateOnCurrentDataset)
        {
            return Task.CompletedTask;
        }

        PendingLockActionState = shouldLock;
        PendingLockActionMonth = AppliedMonth;
        PendingLockActionYear = AppliedYear;
        SelectedLockActionScope = CanChooseSelectedRowsLockScope
            ? LockScopeSelectedRows
            : LockScopeWholePeriod;
        IsLockActionPopupVisible = true;
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnLockActionPopupVisibleChangedAsync</c>.</summary>
    private Task OnLockActionPopupVisibleChangedAsync(bool visible)
    {
        if (!IsProcessingScreenAction)
        {
            IsLockActionPopupVisible = visible;
        }

        return Task.CompletedTask;
    }

    /// <summary>Cập nhật lựa chọn cho luồng <c>SelectLockActionScopeAsync</c>.</summary>
    private Task SelectLockActionScopeAsync(string scope)
    {
        if (IsProcessingScreenAction)
        {
            return Task.CompletedTask;
        }

        if (string.Equals(scope, LockScopeSelectedRows, StringComparison.Ordinal) && CanChooseSelectedRowsLockScope)
        {
            SelectedLockActionScope = LockScopeSelectedRows;
        }
        else if (string.Equals(scope, LockScopeWholePeriod, StringComparison.Ordinal))
        {
            SelectedLockActionScope = LockScopeWholePeriod;
        }

        return Task.CompletedTask;
    }

    /// <summary>Xác nhận cho luồng <c>ConfirmLockActionAsync</c>.</summary>
    private async Task ConfirmLockActionAsync()
    {
        if (!CanConfirmLockAction)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một dòng hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
            return;
        }

        var shouldLock = PendingLockActionState;
        var actionText = shouldLock ? "khóa" : "mở khóa";
        var isWholePeriod = IsWholePeriodLockScope;
        var payrollPeriod = PendingLockActionPeriodDisplay;
        IReadOnlyList<LeaveHolidayAllowanceRecord> selectedRows = isWholePeriod ? [] : GetSelectedVisibleRecords();
        var targetSummaryRecordIds = isWholePeriod
            ? null
            : selectedRows.Select(row => row.Id).Where(id => id != Guid.Empty).Distinct().ToArray();

        if (!isWholePeriod && (targetSummaryRecordIds is null || targetSummaryRecordIds.Length == 0))
        {
            ToastService.ShowWarning("Không còn dòng hợp lệ đang chọn. Hãy chọn lại hoặc chuyển sang phạm vi toàn bộ kỳ lương.");
            return;
        }

        if (shouldLock
            && IsManualEditPopupVisible
            && ManualEditModel is not null
            && (isWholePeriod || targetSummaryRecordIds!.Contains(ManualEditModel.Id)))
        {
            ResetManualEditPopupState();
        }

        try
        {
            IsLockActionPopupVisible = false;
            await RunScreenActionAsync(
                isWholePeriod
                    ? $"Đang {actionText} dữ liệu phụ cấp Phép - Lễ kỳ {payrollPeriod}..."
                    : $"Đang {actionText} {targetSummaryRecordIds!.Length:N0} dòng phụ cấp Phép - Lễ...",
                async () =>
                {
                    var result = await ExecuteDataOperationAsync(
                        token => DataProvider.SetLockStateBatchAsync(
                            new SetLeaveHolidayAllowanceBatchLockStateRequest(
                                PendingLockActionYear,
                                PendingLockActionMonth,
                                shouldLock,
                                targetSummaryRecordIds),
                            token),
                        disposalTokenSource.Token);

                    if (result.TargetRowCount == 0)
                    {
                        ToastService.ShowInfo(
                            result.SkippedCount > 0
                                ? $"Không có dòng phụ cấp Phép - Lễ hợp lệ để {actionText}; đã bỏ qua {result.SkippedCount:N0} ID không tồn tại hoặc không thuộc kỳ {payrollPeriod}."
                                : $"Không có dòng phụ cấp Phép - Lễ hợp lệ trong phạm vi đã chọn để {actionText}.");
                        return;
                    }

                    if (result.UpdatedCount == 0)
                    {
                        ToastService.ShowInfo(
                            shouldLock
                                ? $"Không có dòng nào cần khóa. {result.TargetRowCount:N0} dòng đã ở trạng thái khóa{FormatBatchSkippedCount(result.SkippedCount)}."
                                : $"Không có dòng nào cần mở khóa. {result.TargetRowCount:N0} dòng đã ở trạng thái mở{FormatBatchSkippedCount(result.SkippedCount)}.");
                        return;
                    }

                    await ReloadSnapshotAfterBatchActionAsync();
                    ToastService.ShowSuccess(
                        BuildBatchLockStateSuccessMessage(
                            shouldLock,
                            payrollPeriod,
                            result.TargetRowCount,
                            result.UpdatedCount,
                            result.SkippedCount,
                            isWholePeriod));
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
            ToastService.ShowError($"Không thể {actionText} dữ liệu phụ cấp Phép - Lễ của kỳ {payrollPeriod}.");
        }
    }

    #endregion
}
