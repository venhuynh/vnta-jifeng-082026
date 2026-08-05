using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTongHop.Policies;

public sealed class PayrollAllowanceSummaryManualAdjustmentPolicyTests
{
    [Fact]
    public void ValidateAndNormalize_rejects_negative_manual_allowance_amount()
    {
        var exception = Assert.Throws<PayrollAllowanceSummaryValidationException>(() =>
            PayrollAllowanceSummaryManualAdjustmentPolicy.ValidateAndNormalize(
                new UpdatePayrollAllowanceSummaryManualValuesRequest(
                    Guid.NewGuid(), -1m, 0m, 0m, 0m, 0m, 0m, 0m, 0m,
                    null, IsLocked: true, OriginalUpdatedAtUtc: null, Actor: "tester")));

        Assert.Contains("trách nhiệm", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateAndNormalizeNote_trims_a_normal_manual_note()
    {
        var note = PayrollAllowanceSummaryManualAdjustmentPolicy.ValidateAndNormalizeNote(
            new UpdatePayrollAllowanceSummaryManualNoteRequest(Guid.NewGuid(), "  Kiểm tra lại phụ cấp  ", null, "tester"));

        Assert.Equal("Kiểm tra lại phụ cấp", note);
    }

    [Fact]
    public void ValidateAndNormalizeNote_accepts_the_1000_character_boundary_and_turns_whitespace_into_null()
    {
        var atBoundary = PayrollAllowanceSummaryManualAdjustmentPolicy.ValidateAndNormalizeNote(
            new UpdatePayrollAllowanceSummaryManualNoteRequest(Guid.NewGuid(), new string('a', 1_000), null, "tester"));
        var whitespace = PayrollAllowanceSummaryManualAdjustmentPolicy.ValidateAndNormalizeNote(
            new UpdatePayrollAllowanceSummaryManualNoteRequest(Guid.NewGuid(), "   ", null, "tester"));

        Assert.Equal(1_000, atBoundary!.Length);
        Assert.Null(whitespace);
    }

    [Theory]
    [InlineData(1001)]
    [InlineData(1200)]
    public void ValidateAndNormalizeNote_rejects_notes_over_the_business_limit(int length)
    {
        var exception = Assert.Throws<PayrollAllowanceSummaryValidationException>(() =>
            PayrollAllowanceSummaryManualAdjustmentPolicy.ValidateAndNormalizeNote(
                new UpdatePayrollAllowanceSummaryManualNoteRequest(Guid.NewGuid(), new string('a', length), null, "tester")));

        Assert.Contains("1000", exception.Message);
    }

    [Fact]
    public void ValidateAndNormalizeNote_rejects_a_missing_summary_identity()
    {
        Assert.Throws<PayrollAllowanceSummaryValidationException>(() =>
            PayrollAllowanceSummaryManualAdjustmentPolicy.ValidateAndNormalizeNote(
                new UpdatePayrollAllowanceSummaryManualNoteRequest(Guid.Empty, "note", null, "tester")));
    }
}
