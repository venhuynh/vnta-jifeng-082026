using DevExpress.Blazor;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>Đại diện kiểu <c>PhuCapDocHai</c> phục vụ màn hình phụ cấp độc hại.</summary>
public partial class PhuCapDocHai
{
    #region Xuất dữ liệu

    // DevExpress export render trong circuit; vượt ngưỡng này phải chuyển sang job nền có file storage bền vững.
    private const int MaximumSynchronousExportRowCount = 10_000;

    /// <summary>Xuất Excel toàn bộ tập filter server-side hiện hành.</summary>
    private Task ExportAllDataToExcel() => ExportCurrentFilterAsync(
        () => ExportSource!.ExportToExcelAsync(BuildExportFileName()),
        "Excel");

    /// <summary>Xuất Excel các dòng đang chọn trong trang grid hiện tại.</summary>
    private Task ExportSelectedRowsToExcel() => ExportGridAsync(
        () => Grid!.ExportToXlsxAsync(
            "hazard-allowance-results-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất Excel cho các dòng phụ cấp độc hại đã chọn.");

    /// <summary>Xuất PDF toàn bộ tập filter server-side hiện hành.</summary>
    private Task ExportAllDataToPdf() => ExportCurrentFilterAsync(
        () => ExportSource!.ExportToPdfAsync(BuildExportFileName()),
        "PDF");

    private async Task QueueBackgroundCsvExportAsync()
    {
        if(!CanExport)
        {
            ToastService.ShowWarning("Chưa có dữ liệu phụ cấp độc hại để xuất.");
            return;
        }

        try
        {
            var job = await ExecuteDataOperationAsync(
                cancellationToken => DataProvider.QueueExportJobAsync(BuildExportFilter(), cancellationToken));
            ToastService.ShowInfo($"Đã xếp hàng export CSV nền ({job.Id:D}). Tệp sẽ tự tải khi hoàn tất.");
            _ = TrackBackgroundExportAsync(job.Id, disposalTokenSource.Token);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể xếp hàng export CSV nền phụ cấp độc hại.");
            ToastService.ShowError("Không thể xếp hàng export CSV nền.");
        }
    }

    /// <summary>Xuất PDF các dòng đang chọn trong trang grid hiện tại.</summary>
    private Task ExportSelectedRowsToPdf() => ExportGridAsync(
        () => Grid!.ExportToPdfAsync(
            "hazard-allowance-results-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true }),
        "Đã bắt đầu xuất PDF cho các dòng phụ cấp độc hại đã chọn.");

    /// <summary>Chạy export trực tiếp từ grid đang render và chuyển lỗi thành toast thân thiện.</summary>
    private async Task ExportGridAsync(Func<Task> exportAction, string successMessage)
    {
        if(Grid is null)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể xuất dữ liệu phụ cấp độc hại từ lưới hiện tại.");
            ToastService.ShowError("Không thể xuất dữ liệu phụ cấp độc hại.");
        }
    }

    /// <summary>Chuẩn bị hidden export grid bằng toàn bộ dữ liệu server trước khi tạo file.</summary>
    private async Task ExportCurrentFilterAsync(Func<Task> exportAction, string format)
    {
        if(!CanExport)
        {
            ToastService.ShowWarning("Chưa có dữ liệu phụ cấp độc hại để xuất.");
            return;
        }

        if(LoadedRecords.Count > MaximumSynchronousExportRowCount)
        {
            ToastService.ShowWarning(
                $"Tập xuất có {LoadedRecords.Count:N0} dòng, vượt ngưỡng {MaximumSynchronousExportRowCount:N0} dòng của export trực tiếp.");
            return;
        }

        try
        {
            IsExporting = true;
            LoadingText = $"Đang chuẩn bị xuất {format} phụ cấp độc hại kỳ {AppliedPeriodLabel}...";
            ExportRecords = await ExecuteDataOperationAsync(
                cancellationToken => DataProvider.ExportAsync(BuildExportFilter(), cancellationToken));
            if(ExportRecords.Count == 0)
            {
                ToastService.ShowInfo("Không có dữ liệu phụ cấp độc hại phù hợp với bộ lọc hiện tại để xuất file.");
                return;
            }

            exportGridRenderCompletionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await InvokeAsync(StateHasChanged);
            await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);

            if(ExportSource is null)
            {
                throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
            }

            await exportAction();
            ToastService.ShowSuccess($"Đã bắt đầu xuất {format} toàn bộ phụ cấp độc hại theo bộ lọc hiện tại.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã dispose nên không hiển thị toast cho thao tác đã bị hủy bình thường.
        }
        catch(Exception ex)
        {
            Logger.LogError(ex, "Không thể xuất {ExportFormat} phụ cấp độc hại cho kỳ {PayrollMonth}/{PayrollYear}.", format, AppliedMonth, AppliedYear);
            ToastService.ShowError($"Không thể xuất {format} phụ cấp độc hại.");
        }
        finally
        {
            ExportRecords = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;
            LoadingText = DefaultLoadingText;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task TrackBackgroundExportAsync(Guid jobId, CancellationToken cancellationToken)
    {
        while(!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            var job = await ExecuteDataOperationAsync(
                token => DataProvider.GetExportJobAsync(jobId, token));
            if(job is null)
            {
                return;
            }

            if(job.Status == HazardAllowanceExportJobStatus.Completed)
            {
                await InvokeAsync(() => ToastService.ShowSuccess("Export CSV nền đã hoàn tất, đang tải tệp."));
                await InvokeAsync(() => NavigationManager.NavigateTo(
                    $"/api/payroll/hazard-allowance/export-jobs/{jobId:D}/download",
                    forceLoad: true));
                return;
            }

            if(job.Status == HazardAllowanceExportJobStatus.Failed)
            {
                await InvokeAsync(() => ToastService.ShowError(
                    string.IsNullOrWhiteSpace(job.ErrorMessage)
                        ? "Export CSV nền thất bại."
                        : job.ErrorMessage));
                return;
            }
        }
    }

    /// <summary>Tạo tên file ổn định theo kỳ đã áp dụng, không dùng kỳ toolbar chưa commit.</summary>
    private string BuildExportFileName() =>
        $"hazard-allowance-{AppliedYear:D4}-{AppliedMonth:D2}";

    #endregion
}
