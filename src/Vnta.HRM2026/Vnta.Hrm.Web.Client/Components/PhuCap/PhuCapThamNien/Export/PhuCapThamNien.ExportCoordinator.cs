namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

/// <summary>Coordinator use case for grid interaction and full-period exports.</summary>
public partial class PhuCapThamNien
{
    #region Tương tác lưới và xuất dữ liệu

    /// <summary>Mở bộ chọn cột của lưới dữ liệu chính.</summary>
    private Task OnColumnChooserRequested()
    {
        GridSection?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    /// <summary>Đồng bộ các đối tượng được chọn từ lưới vào trạng thái component.</summary>
    private Task OnSelectedDataItemsChanged(IReadOnlyList<object> items)
    {
        SelectedDataItems = items;
        return Task.CompletedTask;
    }


    /// <summary>Xuất toàn bộ dữ liệu của kỳ đang áp dụng sang tệp Excel.</summary>
    private Task ExportAllDataToExcelAsync() => ExportAllForAppliedPeriodAsync(
        () => ExportSource!.ExportToExcelAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu phụ cấp thâm niên kỳ {AppliedPeriodLabel} ra Excel.");

    /// <summary>Xuất toàn bộ dữ liệu của kỳ đang áp dụng sang tệp PDF.</summary>
    private Task ExportAllDataToPdfAsync() => ExportAllForAppliedPeriodAsync(
        () => ExportSource!.ExportToPdfAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu phụ cấp thâm niên kỳ {AppliedPeriodLabel} ra PDF.");

    /// <summary>Tải dữ liệu toàn kỳ, chờ lưới xuất render rồi gọi hành động tạo tệp được truyền vào.</summary>
    private async Task ExportAllForAppliedPeriodAsync(Func<Task> exportAction, string successMessage)
    {
        if(!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsExporting = true;
        SetLoadingText($"Đang chuẩn bị toàn bộ dữ liệu phụ cấp thâm niên kỳ {AppliedPeriodLabel} để xuất file...");
        try
        {
            ExportRecords = await DataProvider.LoadAllForPeriodExportAsync(
                AppliedYear,
                AppliedMonth,
                disposalTokenSource.Token);
            if(ExportRecords.Count == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu phụ cấp thâm niên của kỳ {AppliedPeriodLabel} để xuất file.");
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
            ToastService.ShowSuccess(successMessage);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã được dispose; không hiển thị lỗi cho người dùng.
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể xuất dữ liệu phụ cấp thâm niên của kỳ {AppliedPeriodLabel}.");
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

    #endregion

}
