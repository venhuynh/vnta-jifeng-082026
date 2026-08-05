using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

public partial class PhuCapTrachNhiem
{
    private Task ExportAllDataToExcelAsync() => ExportCurrentPeriodAsync(
        () => ExportGrid!.ExportToXlsxAsync(BuildExportFileName()),
        "Excel");

    private Task ExportAllDataToPdfAsync() => ExportCurrentPeriodAsync(
        () => ExportGrid!.ExportToPdfAsync(BuildExportFileName()),
        "PDF");

    private async Task ExportCurrentPeriodAsync(Func<Task> exportAction, string fileFormat)
    {
        if (!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        var exported = false;
        try
        {
            await RunBusyAsync(
                $"Đang chuẩn bị xuất toàn bộ dữ liệu phụ cấp trách nhiệm kỳ {CurrentPeriodLabel}...",
                async () =>
                {
                    ExportRows = await AbcQueryProvider.ExportAsync(
                        new PayrollResponsibilityAllowanceAbcExportRequest(
                            AppliedYear,
                            AppliedMonth,
                            string.Equals(fileFormat, "Excel", StringComparison.OrdinalIgnoreCase) ? "xlsx" : "pdf"),
                        disposalTokenSource.Token);
                    if (ExportRows.Count == 0)
                    {
                        ToastService.ShowInfo($"Không có dữ liệu phụ cấp trách nhiệm kỳ {CurrentPeriodLabel} để xuất file.");
                        return;
                    }

                    exportGridRenderCompletionSource = new TaskCompletionSource<bool>(
                        TaskCreationOptions.RunContinuationsAsynchronously);
                    await InvokeAsync(StateHasChanged);
                    await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);

                    if (ExportGrid is null)
                    {
                        throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
                    }

                    await exportAction();
                    exported = true;
                });

            if (exported)
            {
                ToastService.ShowInfo(
                    $"Đã bắt đầu xuất toàn bộ dữ liệu phụ cấp trách nhiệm kỳ {CurrentPeriodLabel} ra {fileFormat}.");
            }
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu phụ cấp trách nhiệm.");
        }
        finally
        {
            ExportRows = [];
            exportGridRenderCompletionSource = null;

            if (!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private string BuildExportFileName() => $"responsibility-allowances-{AppliedYear:D4}-{AppliedMonth:D2}";
}
