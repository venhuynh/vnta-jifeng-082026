using DevExpress.Blazor;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

public sealed partial class OtherAllowanceCoordinator
{
    private async Task SyncFromPreviousMonthCoreAsync()
    {
        if(!CanSyncFromPreviousMonth || disposalTokenSource.IsCancellationRequested) return;

        var targetPayrollMonth = AppliedMonth;
        var targetPayrollYear = AppliedYear;
        var sourcePeriod = GetPreviousPeriod(targetPayrollMonth, targetPayrollYear);
        var sourcePeriodDisplay = $"{sourcePeriod.Month:00}/{sourcePeriod.Year}";
        var targetPeriodDisplay = $"{targetPayrollMonth:00}/{targetPayrollYear}";
        var confirmed = await DialogService.ConfirmAsync(
            $"Hệ thống sẽ lấy dữ liệu phụ cấp khác từ kỳ {sourcePeriodDisplay} sang kỳ {targetPeriodDisplay}. " +
            "Các dòng Cố định đã có sẽ được cập nhật theo kỳ nguồn; summary hoặc dòng đã khóa ở kỳ đích vẫn được giữ nguyên. Bạn có muốn tiếp tục?",
            title: "Lấy từ tháng trước",
            okText: "Lấy dữ liệu",
            cancelText: "Hủy",
            renderStyle: MessageBoxRenderStyle.Primary);
        if(!confirmed || !CanSyncFromPreviousMonth || disposalTokenSource.IsCancellationRequested) return;

        try
        {
            IsSyncingFromPreviousMonth = true;
            LoadingText = $"Đang lấy phụ cấp khác từ kỳ {sourcePeriodDisplay} sang {targetPeriodDisplay}...";
            var result = await PreviousMonthSyncDataProvider.SyncFromPreviousMonthAsync(
                targetPayrollMonth,
                targetPayrollYear,
                disposalTokenSource.Token);
            await LoadAsync();
            ShowPreviousMonthSyncResult(result);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(UnauthorizedAccessException)
        {
            ToastService.ShowWarning("Bạn không có quyền lấy dữ liệu phụ cấp khác từ tháng trước.");
        }
        catch(HrmApiException exception) when(exception.Kind is HrmApiErrorKind.Unauthenticated or HrmApiErrorKind.Forbidden)
        {
            ToastService.ShowWarning("Bạn không có quyền lấy dữ liệu phụ cấp khác từ tháng trước.");
        }
        catch(InvalidOperationException exception)
        {
            ToastService.ShowWarning(exception.Message);
        }
        catch(Exception exception)
        {
            Logger.LogError(
                exception,
                "Không thể lấy phụ cấp khác từ kỳ {SourcePayrollMonth}/{SourcePayrollYear} sang kỳ {TargetPayrollMonth}/{TargetPayrollYear}.",
                sourcePeriod.Month,
                sourcePeriod.Year,
                targetPayrollMonth,
                targetPayrollYear);
            ToastService.ShowError("Không thể lấy dữ liệu phụ cấp khác từ tháng trước. Vui lòng thử lại.");
        }
        finally
        {
            if(!disposalTokenSource.IsCancellationRequested)
            {
                IsSyncingFromPreviousMonth = false;
                LoadingText = DefaultLoadingText;
            }
        }
    }

    private void ShowPreviousMonthSyncResult(SyncOtherAllowanceFromPreviousMonthResult result)
    {
        var sourcePeriodDisplay = $"{result.SourcePayrollMonth:00}/{result.SourcePayrollYear}";
        var targetPeriodDisplay = $"{result.TargetPayrollMonth:00}/{result.TargetPayrollYear}";
        if(result.SourceRowCount == 0)
        {
            ToastService.ShowInfo($"Không có dòng phụ cấp khác ở kỳ {sourcePeriodDisplay} để lấy sang kỳ {targetPeriodDisplay}.");
            return;
        }

        if(result.CreatedCount == 0 && result.UpdatedFixedCount == 0)
        {
            ToastService.ShowInfo(BuildNoRowsCreatedMessage(result, targetPeriodDisplay));
            return;
        }

        var details = BuildSyncDetailMessages(result);
        ToastService.ShowSuccess(
            $"Đã đồng bộ {result.CreatedCount + result.UpdatedFixedCount:N0}/{result.SourceRowCount:N0} dòng phụ cấp khác từ kỳ {sourcePeriodDisplay} sang kỳ {targetPeriodDisplay}"
            + (details.Count == 0 ? "." : $", {string.Join(", ", details)}."));
    }

    private static string BuildNoRowsCreatedMessage(SyncOtherAllowanceFromPreviousMonthResult result, string targetPeriodDisplay)
    {
        var details = BuildSyncDetailMessages(result);
        return details.Count == 0
            ? $"Không có dòng phụ cấp khác nào cần thêm cho kỳ {targetPeriodDisplay}."
            : $"Không thêm dòng phụ cấp khác nào cho kỳ {targetPeriodDisplay}: {string.Join(", ", details)}.";
    }

    private static List<string> BuildSyncDetailMessages(SyncOtherAllowanceFromPreviousMonthResult result)
    {
        var details = new List<string>();
        if(result.CreatedCount > 0) details.Add($"thêm {result.CreatedCount:N0} dòng mới");
        if(result.UpdatedFixedCount > 0) details.Add($"cập nhật {result.UpdatedFixedCount:N0} dòng Cố định");
        if(result.SkippedExistingCount > 0) details.Add($"giữ nguyên {result.SkippedExistingCount:N0} dòng đã có");
        if(result.SkippedTargetSummaryLockedCount > 0) details.Add($"bỏ qua {result.SkippedTargetSummaryLockedCount:N0} dòng có summary đích đã khóa");
        if(result.SkippedTargetDetailLockedCount > 0) details.Add($"bỏ qua {result.SkippedTargetDetailLockedCount:N0} dòng đích đã khóa");
        if(result.SkippedMissingTargetSummaryCount > 0) details.Add($"bỏ qua {result.SkippedMissingTargetSummaryCount:N0} dòng chưa có summary đích");
        return details;
    }
}
