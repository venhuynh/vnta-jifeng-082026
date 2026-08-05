namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

public partial class PhuCapPhepLe
{
    private Task ExportAllDataToExcel() => ExportAllGridDataAsync(
        () => ExportGridSection!.ExportToExcelAsync(),
        "Đã bắt đầu xuất Excel phụ cấp Phép - Lễ.");

    private Task ExportSelectedRowsToExcel() => ExportGridDataAsync(
        () => GridSection!.ExportSelectedToExcelAsync(),
        "Đã bắt đầu xuất Excel cho các dòng phụ cấp Phép - Lễ đã chọn.");

    private Task ExportAllDataToPdf() => ExportAllGridDataAsync(
        () => ExportGridSection!.ExportToPdfAsync(),
        "Đã bắt đầu xuất PDF phụ cấp Phép - Lễ.");

    private Task ExportSelectedRowsToPdf() => ExportGridDataAsync(
        () => GridSection!.ExportSelectedToPdfAsync(),
        "Đã bắt đầu xuất PDF cho các dòng phụ cấp Phép - Lễ đã chọn.");

    private Task ExportAllGridDataAsync(Func<Task> exportAction, string successMessage) =>
        ExportGridDataAsync(ExportGridSection is not null, exportAction, successMessage);

    private Task ExportGridDataAsync(Func<Task> exportAction, string successMessage) =>
        ExportGridDataAsync(GridSection is not null, exportAction, successMessage);

    private async Task ExportGridDataAsync(bool isGridReady, Func<Task> exportAction, string successMessage)
    {
        if (!CanOperateOnCurrentDataset) return;
        if (!isGridReady)
        {
            ToastService.ShowWarning("Lưới dữ liệu chưa sẵn sàng để xuất.");
            return;
        }

        try
        {
            await exportAction();
            ToastService.ShowInfo(successMessage);
        }
        catch (Exception)
        {
            ToastService.ShowError("Không thể xuất dữ liệu phụ cấp Phép - Lễ.");
        }
    }
}
