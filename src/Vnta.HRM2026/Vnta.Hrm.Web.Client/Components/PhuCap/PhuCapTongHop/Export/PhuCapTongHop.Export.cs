using DevExpress.Blazor;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Owns complete-period export through the hidden export grid.</summary>
public partial class PhuCapTongHop
{
    private Task ExportAllDataToExcelAsync() => ExportAllForAppliedPeriodAsync(
        PayrollAllowanceSummaryExportFormat.Excel,
        () => ExportGrid!.ExportToXlsxAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu tổng hợp phụ cấp kỳ {CurrentPayrollPeriodDisplay} ra Excel.");

    private Task ExportAllDataToPdfAsync() => ExportAllForAppliedPeriodAsync(
        PayrollAllowanceSummaryExportFormat.Pdf,
        () => ExportGrid!.ExportToPdfAsync(BuildExportFileName(), new GridPdfExportOptions { FitToPage = true, CustomizeDocument = args => args.Landscape = true }),
        $"Đã xuất toàn bộ dữ liệu tổng hợp phụ cấp kỳ {CurrentPayrollPeriodDisplay} ra PDF.");

    private async Task ExportAllForAppliedPeriodAsync(PayrollAllowanceSummaryExportFormat format, Func<Task> exportAction, string successMessage)
    {
        if(!CanExport || disposalTokenSource.IsCancellationRequested) return;
        IsExporting = true;
        CurrentActionLoadingText = $"Đang chuẩn bị toàn bộ dữ liệu tổng hợp phụ cấp kỳ {CurrentPayrollPeriodDisplay} để xuất file...";
        await RenderBusyStateAsync();
        try
        {
            ExportRecords = await DataProvider.LoadAllForPeriodExportAsync(AppliedMonth, AppliedYear, format, disposalTokenSource.Token);
            if(ExportRecords.Count == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu tổng hợp phụ cấp của kỳ {CurrentPayrollPeriodDisplay} để xuất file.");
                return;
            }

            exportGridRenderCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            await InvokeAsync(StateHasChanged);
            await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);
            if(ExportGrid is null) throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
            await exportAction();
            ToastService.ShowSuccess(successMessage);
        }
        catch(OperationCanceledException)
        {
            if(!disposalTokenSource.IsCancellationRequested) throw;
        }
        catch(Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu tổng hợp phụ cấp.");
        }
        finally
        {
            ExportRecords = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;
            CurrentActionLoadingText = null;
            await InvokeAsync(StateHasChanged);
        }
    }

    private string BuildExportFileName() => $"phu-cap-tong-hop-{AppliedYear}-{AppliedMonth:00}";
}
