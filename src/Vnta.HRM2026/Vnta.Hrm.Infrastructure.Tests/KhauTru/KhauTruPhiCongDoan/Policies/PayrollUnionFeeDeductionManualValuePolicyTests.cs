using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruPhiCongDoan.Policies;

public sealed class PayrollUnionFeeDeductionManualValuePolicyTests
{
    [Fact]
    public void EnsureValid_accepts_supported_amounts()
    {
        PayrollUnionFeeDeductionManualValuePolicy.EnsureValid(0m);
        PayrollUnionFeeDeductionManualValuePolicy.EnsureValid(12.34m);
        PayrollUnionFeeDeductionManualValuePolicy.EnsureValid(PayrollUnionFeeDeductionManualValuePolicy.MaximumAmount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(10000000000000000.00)]
    [InlineData(1.001)]
    public void EnsureValid_rejects_out_of_range_or_over_precision_amounts(decimal amount) =>
        Assert.Throws<InvalidOperationException>(() =>
            PayrollUnionFeeDeductionManualValuePolicy.EnsureValid(amount));

    [Fact]
    public void Normalize_uses_existing_away_from_zero_rounding_rule() =>
        Assert.Equal(1.01m, PayrollUnionFeeDeductionManualValuePolicy.Normalize(1.005m));
}
