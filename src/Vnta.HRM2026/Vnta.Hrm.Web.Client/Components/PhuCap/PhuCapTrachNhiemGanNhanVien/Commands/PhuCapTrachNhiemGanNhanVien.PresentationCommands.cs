using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien;

public partial class PhuCapTrachNhiemGanNhanVien
{
    private void ShowColumnChooser() => Grid?.ShowColumnChooser();

    private string BuildExportFileName() => $"ds-cap-bac-nhan-vien-{AppliedYear}-{AppliedMonth:00}";

    private Task ExportToExcelAsync() => ExportAllAsync(
        "xlsx",
        () => ExportGrid!.ExportToXlsxAsync(BuildExportFileName()),
        "Đã xuất toàn bộ danh sách cấp bậc nhân viên ra Excel.");

    private Task ExportToPdfAsync() => ExportAllAsync(
        "pdf",
        () => ExportGrid!.ExportToPdfAsync(BuildExportFileName()),
        "Đã xuất toàn bộ danh sách cấp bậc nhân viên ra PDF.");

    private async Task ExportAllAsync(string format, Func<Task> exportAction, string successMessage)
    {
        if (!CanExport || disposalTokenSource.IsCancellationRequested)
        {
            return;
        }

        IsExporting = true;
        try
        {
            ExportGrid!.PrepareForRender();
            ExportRecords = await AssignmentProvider.ExportAsync(
                new PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest(AppliedYear, AppliedMonth, format),
                disposalTokenSource.Token);
            await InvokeAsync(StateHasChanged);
            await ExportGrid.WaitForRenderAsync(disposalTokenSource.Token);
            await exportAction();
            ToastService.ShowSuccess(successMessage);
        }
        catch (OperationCanceledException) when (disposalTokenSource.IsCancellationRequested)
        {
        }
        catch
        {
            ToastService.ShowError("Không thể xuất danh sách cấp bậc nhân viên.");
        }
        finally
        {
            ExportRecords = [];
            IsExporting = false;
            if (!disposalTokenSource.IsCancellationRequested)
            {
                await InvokeAsync(StateHasChanged);
            }
        }
    }
}
