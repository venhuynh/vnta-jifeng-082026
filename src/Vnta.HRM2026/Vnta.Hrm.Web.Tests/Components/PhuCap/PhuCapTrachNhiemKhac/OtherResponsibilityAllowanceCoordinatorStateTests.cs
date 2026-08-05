using Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;
using Xunit;

namespace Vnta.Hrm.Web.Tests.Components.PhuCap.PhuCapTrachNhiemKhac;

public sealed class OtherResponsibilityAllowanceCoordinatorStateTests
{
    [Fact]
    public void Initial_contract_keeps_data_actions_disabled_until_a_period_is_loaded()
    {
        using var coordinator = new OtherResponsibilityAllowanceCoordinator(null!, null!, null!);

        coordinator.Initialize(() => Task.CompletedTask);

        Assert.InRange(coordinator.Toolbar.Year, 2026, 2100);
        Assert.Contains(coordinator.Toolbar.Month, coordinator.Toolbar.AvailableMonths.Select(option => option.Value));
        Assert.False(coordinator.Toolbar.CanOpenActionsMenu);
        Assert.False(coordinator.Toolbar.CanExport);
        Assert.False(coordinator.ResultsGrid.CanSearchScreen);
        Assert.Empty(coordinator.ResultsGrid.VisibleRecords);
        Assert.False(coordinator.LoadError.Visible);
    }
}
