using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai.Export;

public partial class PhuCapDocHaiExportGrid
{
    [Parameter, EditorRequired] public IReadOnlyList<HazardAllowanceListItemDto> Records { get; set; } = [];
    [Parameter] public EventCallback Rendered { get; set; }

    private IGrid? ExportGrid { get; set; }

    public Task ExportToExcelAsync(string fileName) => GetGrid().ExportToXlsxAsync(fileName);
    public Task ExportToPdfAsync(string fileName) => GetGrid().ExportToPdfAsync(fileName);

    protected override Task OnAfterRenderAsync(bool firstRender) =>
        Records.Count > 0 ? Rendered.InvokeAsync() : Task.CompletedTask;

    private static string FormatPayrollPeriod(HazardAllowanceListItemDto row) => $"{row.PayrollMonth:00}/{row.PayrollYear}";

    private static string FormatEmployee(HazardAllowanceListItemDto row)
    {
        var employeeCode = row.EmployeeCode?.Trim();
        var employeeName = row.EmployeeName?.Trim();
        return (employeeCode, employeeName) switch
        {
            ({ Length: > 0 }, { Length: > 0 }) => $"{employeeCode} - {employeeName}",
            ({ Length: > 0 }, _) => employeeCode,
            (_, { Length: > 0 }) => employeeName,
            _ => string.Empty
        };
    }

    private static string? FormatPosition(HazardAllowanceListItemDto row) =>
        string.IsNullOrWhiteSpace(row.PositionName) ? null : row.PositionName.Trim();

    private IGrid GetGrid() => ExportGrid ?? throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
}
