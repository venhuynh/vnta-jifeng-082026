using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Handles the immediate, selected-row entitlement actions in the toolbar.</summary>
public partial class PhuCapDocHai
{
    private Task OnExcludeSelectedAsync() => SetSelectedEntitlementAsync(isEligibleForAllowance: false);

    private Task OnIncludeSelectedAsync() => SetSelectedEntitlementAsync(isEligibleForAllowance: true);

    private async Task SetSelectedEntitlementAsync(bool isEligibleForAllowance)
    {
        var selectedRecords = GetSelectedVisibleRecords();
        if(selectedRecords.Count == 0)
        {
            ToastService.ShowWarning("Hãy chọn ít nhất một dòng phụ cấp độc hại.");
            return;
        }
        if(!CanSetSelectedEntitlement)
        {
            ToastService.ShowWarning("Không thể cập nhật trạng thái hưởng khi danh sách chọn có dòng đã khóa.");
            return;
        }

        var targets = selectedRecords
            .Select(record => new HazardAllowanceEntitlementTarget(
                record.PayrollAllowanceSummaryRecordId,
                record.UpdatedAtUtc ?? record.CreatedAtUtc,
                record.SummaryUpdatedAtUtc ?? record.UpdatedAtUtc ?? record.CreatedAtUtc))
            .ToArray();
        var action = isEligibleForAllowance ? "cập nhật hưởng phụ cấp" : "cập nhật ngoại lệ";

        try
        {
            BeginBusyState(isEligibleForAllowance
                ? $"Đang cập nhật hưởng phụ cấp cho {targets.Length:N0} dòng đã chọn..."
                : $"Đang đưa {targets.Length:N0} dòng đã chọn vào ngoại lệ...");
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            var result = await ExecuteDataOperationAsync(
                cancellationToken => DataProvider.SetEntitlementBatchAsync(
                    isEligibleForAllowance,
                    targets,
                    cancellationToken));

            if(isEligibleForAllowance)
            {
                foreach(var record in selectedRecords)
                {
                    await ExecuteDataOperationAsync(
                        cancellationToken => DataProvider.RefreshAsync(
                            record.PayrollMonth,
                            record.PayrollYear,
                            record.PayrollAllowanceSummaryRecordId,
                            cancellationToken));
                }
            }

            await ReloadAsync();
            ToastService.ShowSuccess(result.UpdatedCount == 0
                ? $"{result.TargetRowCount:N0} dòng đã ở trạng thái {(isEligibleForAllowance ? "hưởng phụ cấp" : "ngoại lệ")}."
                : isEligibleForAllowance
                    ? $"Đã cập nhật {result.UpdatedCount:N0}/{result.TargetRowCount:N0} dòng được hưởng phụ cấp độc hại."
                    : $"Đã đưa {result.UpdatedCount:N0}/{result.TargetRowCount:N0} dòng vào ngoại lệ phụ cấp độc hại.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể {Action} phụ cấp độc hại cho kỳ {PayrollMonth}/{PayrollYear}.", action, AppliedMonth, AppliedYear);
            ShowOperationFailure(ex, action);
        }
        finally
        {
            EndBusyState();
        }
    }
}
