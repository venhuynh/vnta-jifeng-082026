using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapKhac.Policies;

public sealed class OtherAllowancePoliciesTests
{
    [Fact]
    public void Normalize_fixed_allowance_trims_business_text_and_keeps_whole_amount()
    {
        var result = OtherAllowanceDefinitionPolicy.Normalize(new OtherAllowanceDefinitionInput(
            "  Trách nhiệm trực ca  ",
            OtherAllowanceAmountType.Fixed,
            125_000m,
            "  Theo quyết định  "));

        Assert.Equal("Trách nhiệm trực ca", result.AllowanceName);
        Assert.Equal(OtherAllowanceAmountType.Fixed, result.AmountType);
        Assert.Equal(125_000m, result.AllowanceAmount);
        Assert.Equal("Theo quyết định", result.Note);
    }

    [Fact]
    public void Normalize_non_fixed_allowance_ignores_entered_amount()
    {
        var result = OtherAllowanceDefinitionPolicy.Normalize(new OtherAllowanceDefinitionInput(
            "Không cố định",
            OtherAllowanceAmountType.NonFixed,
            125_000m,
            null));

        Assert.Equal(0m, result.AllowanceAmount);
    }

    [Theory]
    [InlineData(10.4, 10)]
    [InlineData(10.5, 11)]
    [InlineData(10.6, 11)]
    public void Characterization_fixed_allowance_rounds_to_whole_currency_away_from_zero(decimal enteredAmount, decimal expectedAmount)
    {
        var result = OtherAllowanceDefinitionPolicy.Normalize(new OtherAllowanceDefinitionInput(
            "Làm tròn", OtherAllowanceAmountType.Fixed, enteredAmount, null));

        Assert.Equal(expectedAmount, result.AllowanceAmount);
    }

    [Theory]
    [InlineData(OtherAllowanceAmountType.Fixed)]
    [InlineData(OtherAllowanceAmountType.NonFixed)]
    public void Normalize_rejects_negative_entered_amount_for_every_amount_type(OtherAllowanceAmountType amountType) =>
        Assert.Throws<InvalidOperationException>(() => OtherAllowanceDefinitionPolicy.Normalize(
            new OtherAllowanceDefinitionInput("Phụ cấp", amountType, -0.01m, null)));

    [Fact]
    public void Normalize_rejects_blank_or_too_long_allowance_name()
    {
        Assert.Throws<InvalidOperationException>(() => OtherAllowanceDefinitionPolicy.Normalize(
            new OtherAllowanceDefinitionInput(" ", OtherAllowanceAmountType.Fixed, 0m, null)));
        Assert.Throws<InvalidOperationException>(() => OtherAllowanceDefinitionPolicy.Normalize(
            new OtherAllowanceDefinitionInput(new string('a', 257), OtherAllowanceAmountType.Fixed, 0m, null)));
    }

    [Fact]
    public void Normalize_accepts_allowance_name_at_the_256_character_boundary()
    {
        var name = new string('a', 256);

        var result = OtherAllowanceDefinitionPolicy.Normalize(
            new OtherAllowanceDefinitionInput(name, OtherAllowanceAmountType.Fixed, 0m, null));

        Assert.Equal(name, result.AllowanceName);
    }

    [Fact]
    public void Normalize_rejects_unknown_amount_type()
    {
        Assert.Throws<InvalidOperationException>(() => OtherAllowanceDefinitionPolicy.Normalize(
            new OtherAllowanceDefinitionInput("Phụ cấp", (OtherAllowanceAmountType)99, 0m, null)));
    }

    [Fact]
    public void Ensure_can_edit_accepts_unlocked_row_with_current_version()
    {
        var version = new DateTime(2026, 7, 30, 5, 0, 0, DateTimeKind.Utc);

        OtherAllowanceEditPolicy.EnsureCanEdit(new OtherAllowanceEditabilityInput(
            OtherAllowanceLockState.Unlocked,
            OtherAllowanceLockState.Unlocked,
            version,
            version));
    }

    [Theory]
    [InlineData(OtherAllowanceLockState.Locked, OtherAllowanceLockState.Unlocked)]
    [InlineData(OtherAllowanceLockState.Unlocked, OtherAllowanceLockState.Locked)]
    public void Ensure_can_edit_rejects_locked_row_or_summary(
        OtherAllowanceLockState allowanceLockState,
        OtherAllowanceLockState summaryLockState)
    {
        var version = new DateTime(2026, 7, 30, 5, 0, 0, DateTimeKind.Utc);

        Assert.Throws<InvalidOperationException>(() => OtherAllowanceEditPolicy.EnsureCanEdit(
            new OtherAllowanceEditabilityInput(allowanceLockState, summaryLockState, version, version)));
    }

    [Fact]
    public void Ensure_can_edit_rejects_stale_version_with_conflict()
    {
        var actualVersion = new DateTime(2026, 7, 30, 5, 0, 0, DateTimeKind.Utc);

        Assert.Throws<OtherAllowanceConflictException>(() => OtherAllowanceEditPolicy.EnsureCanEdit(
            new OtherAllowanceEditabilityInput(
                OtherAllowanceLockState.Unlocked,
                OtherAllowanceLockState.Unlocked,
                actualVersion,
                actualVersion.AddTicks(-1))));
    }

    [Fact]
    public void Ensure_can_change_lock_state_rejects_locked_summary_before_version_check()
    {
        var version = new DateTime(2026, 7, 30, 5, 0, 0, DateTimeKind.Utc);

        Assert.Throws<InvalidOperationException>(() => OtherAllowanceEditPolicy.EnsureCanChangeLockState(
            OtherAllowanceLockState.Locked,
            new OtherAllowanceVersionInput(version, version.AddTicks(-1))));
    }

    [Fact]
    public void Calculate_total_adds_all_detail_lines_and_handles_empty_list()
    {
        Assert.Equal(125_001m, OtherAllowanceSummaryAmountCalculator.CalculateTotal(
            [new OtherAllowanceSummaryLine(1m), new OtherAllowanceSummaryLine(125_000m)]));
        Assert.Equal(0m, OtherAllowanceSummaryAmountCalculator.CalculateTotal([]));
    }

    [Fact]
    public void Resolve_actor_trims_actor_or_uses_existing_system_fallback()
    {
        Assert.Equal("hr.admin", OtherAllowanceAuditPolicy.ResolveActor("  hr.admin ").Value);
        Assert.Equal("system", OtherAllowanceAuditPolicy.ResolveActor(" ").Value);
    }
}
