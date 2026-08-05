using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

public partial class PhuCapComExportGrid
{
    private IGrid? grid;

    [Parameter] public IReadOnlyList<MealAllowanceRecord> Records { get; set; } = [];

    public Task ExportToXlsxAsync(string fileName) =>
        grid?.ExportToXlsxAsync(fileName)
        ?? Task.FromException(new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng."));

    public Task ExportToPdfAsync(string fileName) =>
        grid?.ExportToPdfAsync(fileName)
        ?? Task.FromException(new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng."));
}
