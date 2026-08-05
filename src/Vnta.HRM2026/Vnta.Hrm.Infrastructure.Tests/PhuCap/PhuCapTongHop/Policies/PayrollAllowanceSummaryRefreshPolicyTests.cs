using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTongHop.Policies;

/// <summary>
/// Characterization tests for the pure refresh policy. Amounts are intentionally
/// assigned as supplied: no validation, clamping or rounding existed in refresh.
/// </summary>
public sealed class PayrollAllowanceSummaryRefreshPolicyTests
{
    [Fact]
    public void Decide_open_summary_with_changed_sources_applies_every_source_and_preserves_note()
    {
        var sourceAmounts = Amounts(10m, 60m, 20m, 30m, 40m, 50m, 65m, 70m);

        var decision = PayrollAllowanceSummaryRefreshPolicy.Decide(
            new PayrollAllowanceSummaryRefreshInput(
                PayrollAllowanceSummaryLockState.Open,
                PayrollAllowanceSummaryAllowanceAmounts.Empty,
                sourceAmounts,
                "Ghi chú nhập tay"));

        Assert.Equal(PayrollAllowanceSummaryRefreshDisposition.SourceAmountsApplied, decision.Disposition);
        Assert.Equal(sourceAmounts, decision.ResultingAmounts);
        Assert.Equal("Ghi chú nhập tay", decision.PreservedManualNote);
    }

    [Fact]
    public void Decide_open_summary_at_zero_boundary_resets_existing_amounts_to_zero()
    {
        var decision = PayrollAllowanceSummaryRefreshPolicy.Decide(
            new PayrollAllowanceSummaryRefreshInput(
                PayrollAllowanceSummaryLockState.Open,
                Amounts(1m, 1m, 1m, 1m, 1m, 1m, 1m, 1m),
                PayrollAllowanceSummaryAllowanceAmounts.Empty,
                "Giữ nguyên"));

        Assert.Equal(PayrollAllowanceSummaryRefreshDisposition.SourceAmountsApplied, decision.Disposition);
        Assert.Equal(PayrollAllowanceSummaryAllowanceAmounts.Empty, decision.ResultingAmounts);
        Assert.Equal("Giữ nguyên", decision.PreservedManualNote);
    }

    [Fact]
    public void Decide_open_summary_with_unchanged_amounts_does_not_request_an_update()
    {
        var currentAmounts = Amounts(10m, 60m, 20m, 30m, 40m, 50m, 65m, 70m);

        var decision = PayrollAllowanceSummaryRefreshPolicy.Decide(
            new PayrollAllowanceSummaryRefreshInput(
                PayrollAllowanceSummaryLockState.Open,
                currentAmounts,
                currentAmounts,
                "  Ghi chú không bị chuẩn hóa  "));

        Assert.Equal(PayrollAllowanceSummaryRefreshDisposition.NoAllowanceChanges, decision.Disposition);
        Assert.Equal(currentAmounts, decision.ResultingAmounts);
        Assert.Equal("  Ghi chú không bị chuẩn hóa  ", decision.PreservedManualNote);
    }

    [Fact]
    public void Decide_locked_summary_keeps_existing_amounts_even_when_sources_change()
    {
        var currentAmounts = Amounts(10m, 60m, 20m, 30m, 40m, 50m, 65m, 70m);

        var decision = PayrollAllowanceSummaryRefreshPolicy.Decide(
            new PayrollAllowanceSummaryRefreshInput(
                PayrollAllowanceSummaryLockState.Locked,
                currentAmounts,
                Amounts(100m, 600m, 200m, 300m, 400m, 500m, 650m, 700m),
                "Không thay đổi khi khóa"));

        Assert.Equal(PayrollAllowanceSummaryRefreshDisposition.SkippedBecauseLocked, decision.Disposition);
        Assert.Equal(currentAmounts, decision.ResultingAmounts);
        Assert.Equal("Không thay đổi khi khóa", decision.PreservedManualNote);
    }

    [Fact]
    public void Decide_preserves_negative_and_fractional_source_amounts_without_rounding()
    {
        var sourceAmounts = Amounts(-0.005m, 60.125m, 20.333m, 30.666m, 40.999m, 50.001m, -65.555m, 70.777m);

        var decision = PayrollAllowanceSummaryRefreshPolicy.Decide(
            new PayrollAllowanceSummaryRefreshInput(
                PayrollAllowanceSummaryLockState.Open,
                PayrollAllowanceSummaryAllowanceAmounts.Empty,
                sourceAmounts,
                null));

        Assert.Equal(PayrollAllowanceSummaryRefreshDisposition.SourceAmountsApplied, decision.Disposition);
        Assert.Equal(sourceAmounts, decision.ResultingAmounts);
    }

    [Fact]
    public void Decide_rejects_missing_input()
    {
        Assert.Throws<ArgumentNullException>(() => PayrollAllowanceSummaryRefreshPolicy.Decide(null!));
    }

    private static PayrollAllowanceSummaryAllowanceAmounts Amounts(
        decimal responsibility,
        decimal responsibilityOther,
        decimal seniority,
        decimal attendance,
        decimal meal,
        decimal hazard,
        decimal other,
        decimal leaveHoliday) =>
        new(responsibility, responsibilityOther, seniority, attendance, meal, hazard, other, leaveHoliday);
}
