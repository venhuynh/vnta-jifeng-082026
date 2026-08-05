using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>Surface ẩn chỉ phục vụ export; không chứa provider hay workflow.</summary>
public partial class PhuCapTrachNhiemExportGrid
{
    private IGrid? grid;
    [Parameter, EditorRequired] public IReadOnlyList<PayrollResponsibilityAllowanceAbcExportItemDto> Records { get; set; } = [];
    protected override Task OnAfterRenderAsync(bool firstRender) => Rendered.InvokeAsync();
    [Parameter] public EventCallback Rendered { get; set; }
    public Task ExportToXlsxAsync(string fileName) => grid?.ExportToXlsxAsync(fileName) ?? Task.CompletedTask;
    public Task ExportToPdfAsync(string fileName) => grid?.ExportToPdfAsync(fileName) ?? Task.CompletedTask;
}
