using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.KhauTru.KhauTruBHXHYT;

public sealed class PayrollInsuranceDeductionPolicyTests
{
    [Theory]
    [InlineData(false, false, PayrollInsuranceDeductionLockDecision.Allowed)]
    [InlineData(true, false, PayrollInsuranceDeductionLockDecision.Locked)]
    [InlineData(false, true, PayrollInsuranceDeductionLockDecision.Locked)]
    [InlineData(true, true, PayrollInsuranceDeductionLockDecision.Locked)]
    public void Lock_policy_preserves_detail_or_summary_lock_rule(
        bool detailIsLocked,
        bool summaryIsLocked,
        PayrollInsuranceDeductionLockDecision expected)
    {
        var decision = PayrollInsuranceDeductionLockPolicy.Evaluate(
            new PayrollInsuranceDeductionLockInput(detailIsLocked, summaryIsLocked));

        Assert.Equal(expected, decision);
    }

    [Fact]
    public void Concurrency_policy_uses_created_at_when_row_has_not_been_updated()
    {
        var createdAt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Unspecified);

        Assert.True(PayrollInsuranceDeductionConcurrencyPolicy.Matches(
            new PayrollInsuranceDeductionConcurrencyInput(createdAt, null, createdAt)));
    }

    [Fact]
    public void Concurrency_policy_rejects_stale_updated_at()
    {
        var createdAt = new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Unspecified);
        var updatedAt = createdAt.AddMinutes(1);

        Assert.False(PayrollInsuranceDeductionConcurrencyPolicy.Matches(
            new PayrollInsuranceDeductionConcurrencyInput(createdAt, updatedAt, createdAt)));
    }
}
