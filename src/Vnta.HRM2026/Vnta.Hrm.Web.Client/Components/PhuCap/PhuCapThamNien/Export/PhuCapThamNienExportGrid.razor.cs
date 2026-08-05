using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapThamNien;

public partial class PhuCapThamNienExportGrid
{
    [Parameter, EditorRequired] public IReadOnlyList<PhuCapThamNienRecord> Records { get; set; } = [];
    [Parameter, EditorRequired] public Func<decimal?, string> FormatAdministrativeWorkDays { get; set; } = default!;
    [Parameter, EditorRequired] public Func<decimal?, string> FormatWorkDays { get; set; } = default!;
    [Parameter] public EventCallback Rendered { get; set; }

    private IGrid? ExportGrid { get; set; }

    public Task ExportToExcelAsync(string fileName) => GetGrid().ExportToXlsxAsync(fileName);
    public Task ExportToPdfAsync(string fileName) => GetGrid().ExportToPdfAsync(fileName);
    protected override Task OnAfterRenderAsync(bool firstRender) => Records.Count > 0 ? Rendered.InvokeAsync() : Task.CompletedTask;
    private IGrid GetGrid() => ExportGrid ?? throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");
}
