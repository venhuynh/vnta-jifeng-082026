using Vnta.Hrm.Application.PhuCap.PhuCapDashboard.Policies;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapDashboard.Policies;

public sealed class PayrollAllowanceDashboardPolicyTests
{
    [Fact]
    public void Compare_returns_no_data_when_previous_is_zero()
    {
        var result = PayrollAllowanceDashboardMetricPolicy.Compare(100m, 0m);
        Assert.False(result.HasPreviousData);
        Assert.Equal(0d, result.Ratio);
    }

    [Fact]
    public void Compare_preserves_signed_percentage_ratio()
    {
        var result = PayrollAllowanceDashboardMetricPolicy.Compare(125m, 100m);
        Assert.True(result.HasPreviousData);
        Assert.Equal(0.25d, result.Ratio, 10);
    }

    [Fact]
    public void CalculateKpis_handles_empty_overview()
    {
        var result = PayrollAllowanceDashboardMetricPolicy.CalculateKpis(new(0, 0, 0, 100m));
        Assert.Equal(0m, result.AverageAllowance);
        Assert.Equal(0d, result.LockRate);
    }

    [Fact]
    public void Validate_rejects_period_before_data_boundary()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => PayrollAllowanceDashboardPeriodPolicy.Validate(new PayrollAllowanceDashboardFilter(5, 2026)));
        Assert.Contains("06/2026", exception.Message);
    }

    [Fact]
    public void Validate_rejects_invalid_history_and_department_limits()
    {
        Assert.Throws<InvalidOperationException>(() => PayrollAllowanceDashboardPeriodPolicy.Validate(new(7, 2026, 1, 5)));
        Assert.Throws<InvalidOperationException>(() => PayrollAllowanceDashboardPeriodPolicy.Validate(new(7, 2026, 12, 0)));
    }
}
