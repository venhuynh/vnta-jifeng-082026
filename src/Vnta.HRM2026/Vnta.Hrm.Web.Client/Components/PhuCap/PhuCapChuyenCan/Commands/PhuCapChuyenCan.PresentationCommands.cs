using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.Export;
using Vnta.Hrm.Web.Client.Services.Ui;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan;

public partial class PhuCapChuyenCan
{
    /// <summary>Mở cho luồng <c>OpenRulesPopupAsync</c>.</summary>
    private async Task OpenRulesPopupAsync()
    {
        IsRulesPopupVisible = true;
        IsRulesLoading = true;
        AttendanceAllowanceRule = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            AttendanceAllowanceRule = await ReadDataProvider.GetRuleAsync(disposalTokenSource.Token);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy.
        }
        catch(Exception)
        {
            IsRulesPopupVisible = false;
            ToastService.ShowError("Không thể tải cấu hình mã CTL từ server.");
        }
        finally
        {
            IsRulesLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnColumnChooserRequested</c>.</summary>
    private Task OnColumnChooserRequested()
    {
        GridSection?.ShowColumnChooser();
        return Task.CompletedTask;
    }

    /// <summary>Xử lý sự kiện cho luồng <c>OnEmptyStateActionClick</c>.</summary>
    private async Task OnEmptyStateActionClick()
    {
        if(!HasRequestedData || HasPendingPeriodChange)
        {
            await OnViewRequestedAsync();
            return;
        }

        if(CanResetFilters)
        {
            await ResetFiltersAsync();
            return;
        }

        await ReloadAsync();
    }

    /// <summary>Lấy cho luồng <c>GetEmptyStateTitle</c>.</summary>
    private string GetEmptyStateTitle() => !HasRequestedData
        ? "Chưa tải dữ liệu phụ cấp chuyên cần"
        : HasPendingPeriodChange
            ? "Kỳ lương đã thay đổi"
            : CanResetFilters
                ? "Không tìm thấy kết quả phù hợp"
                : "Chưa có dữ liệu phụ cấp chuyên cần";

    /// <summary>Lấy cho luồng <c>GetEmptyStateMessage</c>.</summary>
    private string GetEmptyStateMessage() => !HasRequestedData
        ? "Chọn tháng và năm, rồi nhấn Xem để tải dữ liệu."
        : HasPendingPeriodChange
            ? $"Bạn đã đổi kỳ lương sang {CurrentPeriodLabel}. Nhấn Xem để tải dữ liệu của kỳ này."
            : CanResetFilters
                ? "Hãy điều chỉnh từ khóa tìm kiếm hoặc trạng thái khóa để xem thêm dữ liệu."
                : "Bảng phụ cấp chuyên cần sẽ hiển thị tại đây sau khi có dữ liệu cho kỳ lương đang chọn.";

    /// <summary>Lấy cho luồng <c>GetEmptyStateActionText</c>.</summary>
    private string GetEmptyStateActionText() => !HasRequestedData || HasPendingPeriodChange
        ? "Xem dữ liệu"
        : CanResetFilters
            ? "Đặt lại bộ lọc"
            : "Tải lại";

    /// <summary>Xuất toàn bộ dữ liệu của kỳ đang áp dụng sang Excel.</summary>
    private Task ExportAllDataToExcelAsync() => ExportAllForAppliedPeriodAsync(
        AttendanceAllowanceExportFormat.Excel,
        () => GetExportSource().ExportToExcelAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu phụ cấp chuyên cần kỳ {AppliedPeriodLabel} ra Excel.");

    /// <summary>Xuất toàn bộ dữ liệu của kỳ đang áp dụng sang PDF.</summary>
    private Task ExportAllDataToPdfAsync() => ExportAllForAppliedPeriodAsync(
        AttendanceAllowanceExportFormat.Pdf,
        () => GetExportSource().ExportToPdfAsync(BuildExportFileName()),
        $"Đã xuất toàn bộ dữ liệu phụ cấp chuyên cần kỳ {AppliedPeriodLabel} ra PDF.");

    /// <summary>Tải allowlist toàn kỳ, chờ lưới export render rồi tạo tệp.</summary>
    private async Task ExportAllForAppliedPeriodAsync(
        AttendanceAllowanceExportFormat format,
        Func<Task> exportAction,
        string successMessage)
    {
        if(!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsExporting = true;
        CurrentLoadingText = $"Đang chuẩn bị toàn bộ dữ liệu phụ cấp chuyên cần kỳ {AppliedPeriodLabel} để xuất file...";
        try
        {
            ExportRecords = await ExportDataProvider.ExportAsync(
                AppliedYear,
                AppliedMonth,
                format,
                disposalTokenSource.Token);
            if(ExportRecords.Count == 0)
            {
                ToastService.ShowInfo($"Không có dữ liệu phụ cấp chuyên cần của kỳ {AppliedPeriodLabel} để xuất file.");
                return;
            }

            exportGridRenderCompletionSource = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            await InvokeAsync(StateHasChanged);
            await exportGridRenderCompletionSource.Task.WaitAsync(disposalTokenSource.Token);

            await exportAction();
            ToastService.ShowSuccess(successMessage);
        }
        catch(OperationCanceledException) when(disposalTokenSource.IsCancellationRequested)
        {
            // Component đã bị hủy; không phát thông báo muộn.
        }
        catch(InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch(Exception)
        {
            ToastService.ShowError($"Không thể xuất dữ liệu phụ cấp chuyên cần của kỳ {AppliedPeriodLabel}.");
        }
        finally
        {
            ExportRecords = [];
            exportGridRenderCompletionSource = null;
            IsExporting = false;
            CurrentLoadingText = HrmUiDefaults.LoadingText;
            if(!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}

