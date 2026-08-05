using System.Reflection;
using DevExpress.Blazor;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac.Export;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapTrachNhiemKhac;

public sealed class OtherResponsibilityAllowanceGridExporterTests
{
    [Theory]
    [InlineData(true, "ExportToXlsxAsync")]
    [InlineData(false, "ExportToPdfAsync")]
    public async Task Export_all_uses_the_feature_file_name_and_does_not_limit_the_grid_to_selected_rows(
        bool isExcel,
        string expectedMethod)
    {
        var (grid, proxy) = CreateGrid();
        var exporter = new OtherResponsibilityAllowanceGridExporter();

        if(isExcel)
        {
            await exporter.ExportAllToExcelAsync(grid);
        }
        else
        {
            await exporter.ExportAllToPdfAsync(grid);
        }

        Assert.Equal(expectedMethod, proxy.Method?.Name);
        Assert.Equal("payroll-other-responsibility-allowance", proxy.Arguments![0]);
        Assert.Equal(2, proxy.Arguments.Length);
        Assert.Null(proxy.Arguments[1]);
    }

    [Theory]
    [InlineData(true, "ExportToXlsxAsync")]
    [InlineData(false, "ExportToPdfAsync")]
    public async Task Export_selected_limits_the_export_to_selected_grid_rows(
        bool isExcel,
        string expectedMethod)
    {
        var (grid, proxy) = CreateGrid();
        var exporter = new OtherResponsibilityAllowanceGridExporter();

        if(isExcel)
        {
            await exporter.ExportSelectedToExcelAsync(grid);
        }
        else
        {
            await exporter.ExportSelectedToPdfAsync(grid);
        }

        Assert.Equal(expectedMethod, proxy.Method?.Name);
        Assert.Equal("payroll-other-responsibility-allowance-selected", proxy.Arguments![0]);
        object options = isExcel
            ? Assert.IsType<GridXlExportOptions>(proxy.Arguments[1])
            : Assert.IsType<GridPdfExportOptions>(proxy.Arguments[1]);
        var exportSelectedRowsOnly = options.GetType().GetProperty("ExportSelectedRowsOnly");
        Assert.NotNull(exportSelectedRowsOnly);
        Assert.True((bool)exportSelectedRowsOnly!.GetValue(options)!);
    }

    private static (IGrid Grid, RecordingGridProxy Proxy) CreateGrid()
    {
        var grid = DispatchProxy.Create<IGrid, RecordingGridProxy>();
        return (grid, (RecordingGridProxy)(object)grid);
    }

    private class RecordingGridProxy : DispatchProxy
    {
        public MethodInfo? Method { get; private set; }
        public object?[]? Arguments { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Method = targetMethod;
            Arguments = args;
            return Task.CompletedTask;
        }
    }
}
