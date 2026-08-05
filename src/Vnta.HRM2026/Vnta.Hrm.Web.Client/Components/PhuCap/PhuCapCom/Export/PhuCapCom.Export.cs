using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>Điều phối việc xuất dữ liệu Phụ cấp cơm theo kỳ lương đang áp dụng.</summary>
public partial class PhuCapCom
{
    private Task ExportPeriodToExcelAsync() => ExportCurrentPeriodAsync(
        () => ExportGrid!.ExportToXlsxAsync(BuildExportFileName()),
        "Excel");

    private Task ExportPeriodToPdfAsync() => ExportCurrentPeriodAsync(
        () => ExportGrid!.ExportToPdfAsync(BuildExportFileName()),
        "PDF");

    private async Task ExportCurrentPeriodAsync(
        Func<Task> exportAction,
        string format)
    {
        if(!CanExport || AppliedMonth is not { } month || AppliedYear is not { } year)
        {
            ToastService.ShowWarning("Chưa có kỳ lương đang áp dụng để xuất dữ liệu.");
            return;
        }

        try
        {
            LoadingText = $"Đang chuẩn bị xuất {format} phụ cấp cơm kỳ {month:00}/{year}...";
            IsExporting = true;

            ExportRecords = await DataProvider.ExportPeriodAsync(month, year, disposalTokenSource.Token);
            if(ExportRecords.Count == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu phụ cấp cơm của kỳ {month:00}/{year} để xuất file.");
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
            ToastService.ShowSuccess($"Đã bắt đầu xuất {format} toàn bộ phụ cấp cơm kỳ {month:00}/{year}.");
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã được dispose; không hiển thị lỗi xuất dữ liệu cho người dùng.
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể xuất {format} phụ cấp cơm kỳ {month:00}/{year}.");
        }
        finally
        {
            ExportRecords = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;
            LoadingText = HrmUiDefaults.LoadingText;
            await InvokeAsync(StateHasChanged);
        }
    }

    private string BuildExportFileName() =>
        $"meal-allowance-{AppliedYear:D4}-{AppliedMonth:D2}";
}
