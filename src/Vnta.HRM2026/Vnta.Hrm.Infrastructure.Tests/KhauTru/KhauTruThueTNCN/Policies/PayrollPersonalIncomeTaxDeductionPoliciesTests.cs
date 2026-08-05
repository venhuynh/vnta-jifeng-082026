using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruThueTNCN.Policies;

public sealed class PayrollPersonalIncomeTaxDeductionPoliciesTests
{
    private readonly PayrollPersonalIncomeTaxDeductionManualValuePolicy manualValuePolicy = new();
    private readonly PayrollPersonalIncomeTaxDeductionPeriodPolicy periodPolicy = new();
    private readonly PayrollPersonalIncomeTaxDeductionRefreshPolicy refreshPolicy = new();

    [Theory]
    [InlineData(0)]
    [InlineData(123.45)]
    public void Manual_value_policy_accepts_non_negative_amounts_with_at_most_two_decimals(decimal amount)
    {
        var result = manualValuePolicy.ValidateAndNormalize(new(Guid.NewGuid(), amount, DateTime.UtcNow));
        Assert.Equal(amount, result);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.001)]
    public void Manual_value_policy_rejects_negative_or_over_precision_amounts(decimal amount) =>
        Assert.Throws<InvalidOperationException>(() => manualValuePolicy.ValidateAndNormalize(new(Guid.NewGuid(), amount, DateTime.UtcNow)));

    [Fact]
    public void Manual_value_policy_requires_the_original_concurrency_token() =>
        Assert.Throws<PayrollPersonalIncomeTaxDeductionConflictException>(() => manualValuePolicy.ValidateAndNormalize(new(Guid.NewGuid(), 1m, null)));

    [Theory]
    [InlineData(2026, 1)]
    [InlineData(2100, 12)]
    public void Period_policy_accepts_the_supported_boundaries(int year, int month) => periodPolicy.Validate(year, month);

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(2101, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 13)]
    public void Period_policy_rejects_invalid_periods(int year, int month) =>
        Assert.Throws<InvalidOperationException>(() => periodPolicy.Validate(year, month));

    [Theory]
    [InlineData(false, false, 100, 100, PayrollPersonalIncomeTaxDeductionSynchronizationDecision.Unchanged)]
    [InlineData(false, false, 100, 99, PayrollPersonalIncomeTaxDeductionSynchronizationDecision.UpdateSummary)]
    [InlineData(true, false, 100, 99, PayrollPersonalIncomeTaxDeductionSynchronizationDecision.SkippedLocked)]
    [InlineData(false, true, 100, 99, PayrollPersonalIncomeTaxDeductionSynchronizationDecision.SkippedLocked)]
    public void Refresh_policy_preserves_lock_and_amount_semantics(bool detailLocked, bool summaryLocked, decimal detailAmount, decimal summaryAmount, PayrollPersonalIncomeTaxDeductionSynchronizationDecision expected) =>
        Assert.Equal(expected, refreshPolicy.Decide(detailAmount, summaryAmount, detailLocked, summaryLocked));
}
