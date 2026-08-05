using Bunit;
using DevExpress.Blazor;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;
using Vnta.Hrm.Web.Client.Models.Payroll;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Endpoints.PhuCap.PhuCapCom;

/// <summary>
/// Rendering-level regression tests for the two UI contracts that are easy to
/// break while splitting the page coordinator: grid selection and export data.
/// </summary>
public sealed class PhuCapComRenderingTests
{
    [Fact]
    public async Task Grid_forwards_selected_items_to_the_page_callback()
    {
        using var context = CreateContext();
        var record = CreateRecord();
        IReadOnlyList<object>? receivedSelection = null;
        var initialSelection = new object[] { record };
        var nextSelection = new object[] { record };

        var cut = context.Render<PhuCapComGrid>(parameters => parameters
            .Add(component => component.Records, new[] { record })
            .Add(component => component.SelectedDataItems, initialSelection)
            .Add(component => component.SelectedDataItemsChanged,
                EventCallback.Factory.Create<IReadOnlyList<object>>(
                    context,
                    items => receivedSelection = items)));

        var grid = cut.FindComponent<DxGrid>();
        Assert.Same(initialSelection, grid.Instance.SelectedDataItems);

        await grid.Instance.SelectedDataItemsChanged.InvokeAsync(nextSelection);

        Assert.Same(nextSelection, receivedSelection);
    }

    [Fact]
    public void Export_grid_renders_the_current_rows_before_an_export_is_requested()
    {
        using var context = CreateContext();
        var record = CreateRecord();

        var cut = context.Render<PhuCapComExportGrid>(parameters => parameters
            .Add(component => component.Records, new[] { record }));

        Assert.NotNull(cut.Find(".meal-allowance-export-source"));
        var grid = cut.FindComponent<DxGrid>();
        Assert.Same(record, Assert.Single((IEnumerable)grid.Instance.Data));
    }

    private static BunitContext CreateContext()
    {
        var context = new BunitContext();
        context.Services.AddDevExpressBlazor();
        ConfigureDevExpressModule(context);
        return context;
    }

    private static void ConfigureDevExpressModule(BunitContext context)
    {
        var module = context.JSInterop.SetupModule();
        module.Mode = JSRuntimeMode.Loose;

        var resultType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("DevExpress.Blazor.Internal.GetDeviceInfoResult"))
            .Single(type => type is not null)!;
        var result = Activator.CreateInstance(resultType)!;
        foreach(var property in resultType.GetProperties())
        {
            property.SetValue(result, property.PropertyType == typeof(bool) ? false : 0);
        }

        var setupMethod = typeof(BunitJSInteropSetupExtensions).GetMethods()
            .Single(method => method.Name == "Setup"
                              && method.IsGenericMethodDefinition
                              && method.GetGenericArguments().Length == 1
                              && method.GetParameters().Length == 1);
        var setup = setupMethod.MakeGenericMethod(resultType).Invoke(null, [module])
            ?? throw new InvalidOperationException("Không thể cấu hình JS module DevExpress cho component test.");
        setup.GetType().GetMethod("SetResult")!.Invoke(setup, [result]);
    }

    private static MealAllowanceRecord CreateRecord() => new()
    {
        Id = Guid.Parse("1ee17123-3dc2-4bf7-b11c-6363e70877e8"),
        EmployeeId = Guid.Parse("3730e627-ce23-4a29-8e7c-2db52350a5c8"),
        EmployeeCode = "NV-PCC-001",
        EmployeeName = "Nhân viên kiểm thử",
        PayrollMonth = 6,
        PayrollYear = 2026,
        Overtime1900Days = 1,
        MealAllowancePerQualifiedDay = 18_000m
    };
}
