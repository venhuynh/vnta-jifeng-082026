using System.Globalization;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;

/// <summary>Route host: composes UI sections and delegates all workflow ownership to the coordinator.</summary>
public partial class PhuCapKhac : IDisposable
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    [Inject]
    private IOtherAllowanceScreenController Coordinator { get; set; } = default!;

    private IReadOnlyList<OtherAllowancePageSizeOption> PageSizeOptions =>
        Coordinator.Grid.PageSizeOptions
            .Select(value => new OtherAllowancePageSizeOption(value, value.ToString("N0", DisplayCulture)))
            .ToArray();

    protected override void OnInitialized() => Coordinator.Initialize();

    public void Dispose() => Coordinator.Dispose();
}
