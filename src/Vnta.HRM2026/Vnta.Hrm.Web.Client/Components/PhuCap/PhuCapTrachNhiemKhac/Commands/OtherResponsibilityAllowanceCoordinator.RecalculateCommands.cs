namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

public sealed partial class OtherResponsibilityAllowanceCoordinator
{
    private Task OnRecalculateClickAsync()
    {
        if (CanUseAppliedPeriodActions) IsRecalculateConfirmPopupVisible = true;
        return Task.CompletedTask;
    }

    private void CloseRecalculateConfirmPopup()
    {
        if (!IsRunningScreenAction) IsRecalculateConfirmPopupVisible = false;
    }

    private async Task ConfirmRecalculateCoreAsync()
    {
        if (!CanConfirmRecalculate) return;
        try
        {
            RecalculateOtherResponsibilityAllowanceResult? result = null;
            await RunScreenActionAsync($"Đang tính lại phụ cấp trách nhiệm khác kỳ {AppliedPeriodLabel}...", async () =>
            {
                result = await DataProvider.RecalculateAsync(AppliedYear, AppliedMonth, disposalTokenSource.Token);
                await ReloadAsync();
            });

            if (HasLoadError || result is null) return;
            IsRecalculateConfirmPopupVisible = false;
            ToastService.ShowSuccess(result.RecalculatedCount == 0
                ? "Không có dữ liệu phụ cấp trách nhiệm khác cần tính lại."
                : $"Đã tính lại {result.RecalculatedCount:N0} dòng phụ cấp trách nhiệm khác.");
        }
        catch (OperationCanceledException) when (IsDisposalRequested) { }
        catch (InvalidOperationException)
        {
            ToastService.ShowWarning("Không thể tính lại dữ liệu của kỳ lương này. Hãy kiểm tra trạng thái dữ liệu và thử lại.");
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể tính lại phụ cấp trách nhiệm khác.");
        }
    }
}
