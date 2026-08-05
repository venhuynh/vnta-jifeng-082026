using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Models;

namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.Export;

/// <summary>Owns the hidden grid used to create full-period exports.</summary>
public partial class KhauTruTongKetExportGrid
{
    private IGrid? exportGrid;
    private TaskCompletionSource<bool>? rendered;

    [Parameter, EditorRequired]
    public IReadOnlyList<PayrollDeductionSummaryExportRecord> Records { get; set; } = [];

    public void PrepareForRender() =>
        rendered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WaitUntilReadyAsync(CancellationToken cancellationToken) =>
        (rendered ?? TaskCompletionSourceFactory.Completed).Task.WaitAsync(cancellationToken);

    public Task ExportToExcelAsync(string fileName) =>
        exportGrid?.ExportToXlsxAsync(fileName)
        ?? throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");

    public Task ExportToPdfAsync(string fileName) =>
        exportGrid?.ExportToPdfAsync(fileName)
        ?? throw new InvalidOperationException("Lưới xuất dữ liệu chưa sẵn sàng.");

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        rendered?.TrySetResult(true);
        return base.OnAfterRenderAsync(firstRender);
    }

    private static class TaskCompletionSourceFactory
    {
        public static TaskCompletionSource<bool> Completed { get; } = CreateCompleted();

        private static TaskCompletionSource<bool> CreateCompleted()
        {
            var source = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            source.SetResult(true);
            return source;
        }
    }
}
