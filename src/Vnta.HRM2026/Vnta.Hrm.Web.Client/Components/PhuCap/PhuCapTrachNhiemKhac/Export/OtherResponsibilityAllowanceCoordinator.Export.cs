namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

public sealed partial class OtherResponsibilityAllowanceCoordinator
{
    private Task ExportAllDataToExcelAsync() => ExportAsync(() => GridExporter.ExportAllToExcelAsync(AllowanceGrid!), "Đã bắt đầu xuất Excel phụ cấp trách nhiệm khác.");
    private Task ExportSelectedRowsToExcelAsync() => ExportAsync(() => GridExporter.ExportSelectedToExcelAsync(AllowanceGrid!), "Đã bắt đầu xuất Excel cho các dòng đã chọn.");
    private Task ExportAllDataToPdfAsync() => ExportAsync(() => GridExporter.ExportAllToPdfAsync(AllowanceGrid!), "Đã bắt đầu xuất PDF phụ cấp trách nhiệm khác.");
    private Task ExportSelectedRowsToPdfAsync() => ExportAsync(() => GridExporter.ExportSelectedToPdfAsync(AllowanceGrid!), "Đã bắt đầu xuất PDF cho các dòng đã chọn.");

    private async Task ExportAsync(Func<Task> exportAction, string successMessage)
    {
        if (AllowanceGrid is null || VisibleRecords.Count == 0)
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
            ToastService.ShowError("Không thể xuất dữ liệu phụ cấp trách nhiệm khác.");
        }
    }
}
