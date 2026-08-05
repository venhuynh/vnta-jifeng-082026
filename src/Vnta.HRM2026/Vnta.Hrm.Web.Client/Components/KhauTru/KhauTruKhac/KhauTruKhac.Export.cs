namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruKhac;

public partial class KhauTruKhac
{
    private Task ExportAllDataToExcelAsync() => ExportAllForAppliedPeriodAsync(
        () => ExportGrid!.ExportToXlsxAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu khấu trừ khác kỳ {AppliedPeriodLabel} ra Excel.");

    private Task ExportAllDataToPdfAsync() => ExportAllForAppliedPeriodAsync(
        () => ExportGrid!.ExportToPdfAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu khấu trừ khác kỳ {AppliedPeriodLabel} ra PDF.");

    private async Task ExportAllForAppliedPeriodAsync(Func<Task> exportAction, string successMessage)
    {
        if(!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsExporting = true;
        SetLoadingText($"Đang chuẩn bị toàn bộ dữ liệu khấu trừ khác kỳ {AppliedPeriodLabel} để xuất file...");
        try
        {
            ExportRecords = await DataProvider.LoadAllForPeriodExportAsync(
                AppliedYear,
                AppliedMonth,
                disposalTokenSource.Token);
            if(ExportRecords.Count == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu khấu trừ khác của kỳ {AppliedPeriodLabel} để xuất file.");
                return;
            }

            exportGridRenderCompletionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await InvokeAsync(StateHasChanged);
            await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);

            if(ExportGrid is null)
            {
                throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
            }

            await exportAction();
            ToastService.ShowSuccess(successMessage);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã được dispose; không hiển thị lỗi cho người dùng.
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể xuất dữ liệu khấu trừ khác của kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            ExportRecords = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;
            SetLoadingText(DefaultLoadingText);

            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
