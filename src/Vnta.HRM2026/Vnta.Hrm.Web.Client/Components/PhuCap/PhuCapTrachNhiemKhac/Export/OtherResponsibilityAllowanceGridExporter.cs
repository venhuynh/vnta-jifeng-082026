using DevExpress.Blazor;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Export;

/// <summary>Owns browser-grid export formats; it has no provider or HTTP dependency.</summary>
public sealed class OtherResponsibilityAllowanceGridExporter
{
    public Task ExportAllToExcelAsync(IGrid grid) =>
        grid.ExportToXlsxAsync("payroll-other-responsibility-allowance");

    public Task ExportSelectedToExcelAsync(IGrid grid) =>
        grid.ExportToXlsxAsync(
            "payroll-other-responsibility-allowance-selected",
            new GridXlExportOptions { ExportSelectedRowsOnly = true });

    public Task ExportAllToPdfAsync(IGrid grid) =>
        grid.ExportToPdfAsync("payroll-other-responsibility-allowance");

    public Task ExportSelectedToPdfAsync(IGrid grid) =>
        grid.ExportToPdfAsync(
            "payroll-other-responsibility-allowance-selected",
            new GridPdfExportOptions { ExportSelectedRowsOnly = true });
}
