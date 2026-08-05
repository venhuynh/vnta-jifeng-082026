using Microsoft.Extensions.Logging.Abstractions;
using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Components.PhuCap.PhuCapKhac;

public sealed class OtherAllowanceCoordinatorStateTests
{
    [Fact]
    public void Initialize_exposes_a_valid_read_only_screen_contract_before_data_is_requested()
    {
        using var coordinator = new OtherAllowanceCoordinator(
            null!,
            null!,
            null!,
            null!,
            null!,
            NullLogger<OtherAllowanceCoordinator>.Instance,
            null!,
            null!);

        coordinator.Initialize();

        var toolbar = coordinator.Toolbar;
        var grid = coordinator.Grid;

        Assert.InRange(toolbar.Year, 2026, 2100);
        Assert.Contains(toolbar.Month, toolbar.AvailableMonths);
        Assert.False(toolbar.CanCreate);
        Assert.Empty(grid.Rows);
        Assert.Equal(50, grid.PageSize);
        Assert.Equal(1, grid.TotalPageCount);
    }
}
