using DevExpress.Blazor;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop;

public partial class KhauTruTongKet
{
    private Task ExportAllDataToExcelAsync() => ExportAllForAppliedPeriodAsync(
        PayrollDeductionSummaryExportFormat.Excel,
        () => ExportSection!.ExportToExcelAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay} ra Excel.");

    private Task ExportSelectedRowsToExcel() => ExportAsync(
        () => GridSection!.ExportSelectedToExcelAsync("payroll-deduction-summary-selected"),
        "Đã bắt đầu xuất Excel cho các dòng đã chọn.");

    private Task ExportAllDataToPdfAsync() => ExportAllForAppliedPeriodAsync(
        PayrollDeductionSummaryExportFormat.Pdf,
        () => ExportSection!.ExportToPdfAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay} ra PDF.");

    private Task ExportSelectedRowsToPdf() => ExportAsync(
        () => GridSection!.ExportSelectedToPdfAsync("payroll-deduction-summary-selected"),
        "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if(GridSection is null) { ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất."); return; }
        try { await exportAction(); ToastService.ShowInfo(successMessage); }
        catch(Exception) { ToastService.ShowError("Không thể xuất dữ liệu tổng kết khấu trừ."); }
    }

    private async Task ExportAllForAppliedPeriodAsync(PayrollDeductionSummaryExportFormat format, Func<Task> exportAction, string successMessage)
    {
        if(!CanExport || disposalTokenSource.IsCancellationRequested) return;
        try
        {
            IsExporting = true;
            LoadingText = $"Đang chuẩn bị toàn bộ dữ liệu tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay} để xuất file...";
            ExportRecords = await DataProvider.LoadAllForPeriodExportAsync(AppliedYear, AppliedMonth, format, disposalTokenSource.Token);
            if(ExportRecords.Count == 0) { ToastService.ShowInfo($"Không có dữ liệu tổng kết khấu trừ của kỳ {CurrentPayrollPeriodDisplay} để xuất file."); return; }
            if(ExportSection is null) throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
            ExportSection.PrepareForRender();
            await InvokeAsync(StateHasChanged);
            await ExportSection.WaitUntilReadyAsync(disposalTokenSource.Token);
            await exportAction();
            ToastService.ShowSuccess(successMessage);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested) { }
        catch(Exception) { ToastService.ShowError($"Không thể xuất dữ liệu tổng kết khấu trừ kỳ {CurrentPayrollPeriodDisplay}."); }
        finally
        {
            ExportRecords = [];
            if(!isDisposed)
            {
                IsExporting = false;
                LoadingText = DefaultLoadingText;
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
