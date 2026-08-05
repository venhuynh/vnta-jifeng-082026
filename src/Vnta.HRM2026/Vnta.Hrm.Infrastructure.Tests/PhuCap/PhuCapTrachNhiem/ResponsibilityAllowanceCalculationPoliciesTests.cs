using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem.Policies;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapTongHop;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapTrachNhiem;

public sealed class ResponsibilityAllowanceCalculationPoliciesTests
{
    [Fact]
    public void Abc_model_persists_snapshot_identity_and_requires_its_summary_parent()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=vnta_model_test;Username=model_test;Password=model_test")
            .Options;
        using var dbContext = new ApplicationDbContext(options);
        var entityType = dbContext.Model.FindEntityType(typeof(PayrollResponsibilityAllowanceAbcRow));

        Assert.NotNull(entityType);
        Assert.False(entityType.FindProperty(nameof(PayrollResponsibilityAllowanceAbcRow.EmployeeId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PayrollResponsibilityAllowanceAbcRow.EmployeeCode))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PayrollResponsibilityAllowanceAbcRow.EmployeeName))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PayrollResponsibilityAllowanceAbcRow.PositionName))!.IsNullable);
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                    [
                        nameof(PayrollResponsibilityAllowanceAbcRow.Year),
                        nameof(PayrollResponsibilityAllowanceAbcRow.Month),
                        nameof(PayrollResponsibilityAllowanceAbcRow.PayrollAllowanceSummaryRecordId)
                    ]));
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                    [
                        nameof(PayrollResponsibilityAllowanceAbcRow.Year),
                        nameof(PayrollResponsibilityAllowanceAbcRow.Month),
                        nameof(PayrollResponsibilityAllowanceAbcRow.EmployeeId)
                    ]));
        Assert.Contains(
            entityType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(PayrollAllowanceSummaryRecordRow)
                && foreignKey.Properties.Single().Name == nameof(PayrollResponsibilityAllowanceAbcRow.PayrollAllowanceSummaryRecordId));
    }

    [Fact]
    public void Employee_assignment_is_linked_one_to_one_to_summary_and_allows_an_unassigned_grade()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=vnta_model_test;Username=model_test;Password=model_test")
            .Options;
        using var dbContext = new ApplicationDbContext(options);
        var entityType = dbContext.Model.FindEntityType(typeof(PayrollResponsibilityAllowanceEmployeeAssignmentRow));

        Assert.NotNull(entityType);
        Assert.True(entityType.FindProperty(nameof(PayrollResponsibilityAllowanceEmployeeAssignmentRow.GradeId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PayrollResponsibilityAllowanceEmployeeAssignmentRow.PayrollAllowanceSummaryRecordId))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(PayrollResponsibilityAllowanceEmployeeAssignmentRow.IsAssignGradeFromPosition))!.IsNullable);
        Assert.Equal(true, entityType.FindProperty(nameof(PayrollResponsibilityAllowanceEmployeeAssignmentRow.IsAssignGradeFromPosition))!.GetDefaultValue());
        Assert.Contains(
            entityType.GetForeignKeys(),
            foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(PayrollAllowanceSummaryRecordRow)
                && foreignKey.Properties.Single().Name == nameof(PayrollResponsibilityAllowanceEmployeeAssignmentRow.PayrollAllowanceSummaryRecordId)
                && foreignKey.IsUnique);
        Assert.Contains(
            entityType.GetIndexes(),
            index => index.IsUnique
                && index.Properties.Select(property => property.Name)
                    .SequenceEqual([nameof(PayrollResponsibilityAllowanceEmployeeAssignmentRow.PayrollAllowanceSummaryRecordId)]));
    }

    [Fact]
    public void Workday_metrics_credit_only_eligible_rows_and_round_late_early_deduction()
    {
        var actual = new ResponsibilityAllowanceWorkdayMetricsCalculator().Calculate(
            new ResponsibilityAllowanceWorkdayMetricsInput(
            [
                new(ResponsibilityAllowanceWorkdayEligibility.Eligible, 0m, 0m, new("OT")),
                new(ResponsibilityAllowanceWorkdayEligibility.NotEligible, 0m, 0m, new("KP")),
                new(ResponsibilityAllowanceWorkdayEligibility.Eligible, 721m, 0m, new("HC"))
            ]));

        Assert.Equal(2m, actual.AdministrativeWorkdays);
        Assert.Equal(1.5021m, actual.LateEarlyDeductionDays);
        Assert.Equal(0.4979m, actual.AbcWorkdays);
        Assert.Equal(ResponsibilityAllowanceUnexcusedAbsenceState.Present, actual.UnexcusedAbsenceState);
        Assert.Equal(["HC", "OT"], actual.EligibleAttendanceCodes.Select(x => x.Value));
    }

    [Fact]
    public void Employee_assignment_has_priority_and_invalid_assignment_does_not_fallback_to_position()
    {
        var employeeGrade = new ResponsibilityAllowanceGradeSnapshot(
            Guid.NewGuid(), "E", "Theo nhân viên", 2_000_000m, ResponsibilityAllowanceConfigurationState.Active);
        var positionGrade = new ResponsibilityAllowanceGradeSnapshot(
            Guid.NewGuid(), "P", "Theo chức vụ", 1_000_000m, ResponsibilityAllowanceConfigurationState.Active);
        var grades = new Dictionary<Guid, ResponsibilityAllowanceGradeSnapshot>
        {
            [employeeGrade.Id] = employeeGrade,
            [positionGrade.Id] = positionGrade
        };
        var policy = new ResponsibilityAllowanceSourceSelectionPolicy();

        var selected = policy.Select(new(
            new ResponsibilityAllowanceAssignmentSnapshot(employeeGrade.Id, ResponsibilityAllowanceAssignmentSource.EmployeeAssignment),
            new ResponsibilityAllowancePositionMappingSnapshot(positionGrade.Id, ResponsibilityAllowanceConfigurationState.Active),
            grades));
        var invalid = policy.Select(new(
            new ResponsibilityAllowanceAssignmentSnapshot(Guid.NewGuid(), ResponsibilityAllowanceAssignmentSource.EmployeeAssignment),
            new ResponsibilityAllowancePositionMappingSnapshot(positionGrade.Id, ResponsibilityAllowanceConfigurationState.Active),
            grades));

        Assert.Equal(ResponsibilityAllowanceSelectedSource.EmployeeAssignment, selected.Source);
        Assert.Equal(employeeGrade.Id, selected.Grade!.Id);
        Assert.Equal(ResponsibilityAllowanceSelectedSource.EmployeeAssignment, invalid.Source);
        Assert.Null(invalid.Grade);
    }

    [Fact]
    public void Position_mapping_is_used_only_when_assignment_is_absent_and_active()
    {
        var grade = new ResponsibilityAllowanceGradeSnapshot(
            Guid.NewGuid(), "P", "Theo chức vụ", 1_000_000m, ResponsibilityAllowanceConfigurationState.Active);
        var result = new ResponsibilityAllowanceSourceSelectionPolicy().Select(new(
            null,
            new ResponsibilityAllowancePositionMappingSnapshot(grade.Id, ResponsibilityAllowanceConfigurationState.Active),
            new Dictionary<Guid, ResponsibilityAllowanceGradeSnapshot> { [grade.Id] = grade }));

        Assert.Equal(ResponsibilityAllowanceSelectedSource.PositionDefault, result.Source);
        Assert.Equal(1_000_000m, result.StandardAmount);
    }

    [Fact]
    public void Abc_workdays_subtracts_late_early_minutes_converted_at_480_minutes_per_day_with_four_decimal_places()
    {
        var actual = new ResponsibilityAllowanceWorkdayMetricsCalculator().Calculate(
            new ResponsibilityAllowanceWorkdayMetricsInput(
                Enumerable.Repeat(
                    new ResponsibilityAllowanceWorkdayInput(
                        ResponsibilityAllowanceWorkdayEligibility.Eligible,
                        0m,
                        0m,
                        new("HC")),
                    19)
                .Prepend(new ResponsibilityAllowanceWorkdayInput(
                    ResponsibilityAllowanceWorkdayEligibility.Eligible,
                    721m,
                    0m,
                    new("HC")))
                .ToArray()));

        Assert.Equal(1.5021m, actual.LateEarlyDeductionDays);
        Assert.Equal(18.4979m, actual.AbcWorkdays);
    }

    [Theory]
    [InlineData(26, 25, "A")]
    [InlineData(26, 23, "B")]
    [InlineData(26, 19, "C")]
    [InlineData(26, 18, "D")]
    [InlineData(0, 0, "NA")]
    public void Abc_rating_follows_the_existing_absence_thresholds(
        decimal standardWorkDays,
        decimal actualWorkDays,
        string expectedRating)
    {
        var actual = new ResponsibilityAllowanceAbcPolicy().Evaluate(
            new ResponsibilityAllowanceAbcInput(
                standardWorkDays,
                actualWorkDays,
                ResponsibilityAllowanceUnexcusedAbsenceState.NotPresent));

        Assert.Equal(expectedRating, actual.Rating.ToStorageValue());
    }

    [Fact]
    public void Unexcused_absence_code_kp_forces_rating_c_before_absence_thresholds()
    {
        var actual = new ResponsibilityAllowanceAbcPolicy().Evaluate(
            new ResponsibilityAllowanceAbcInput(
                26m,
                26m,
                ResponsibilityAllowanceUnexcusedAbsenceState.Present));

        Assert.Equal(ResponsibilityAllowanceAbcRating.C, actual.Rating);
    }

    [Fact]
    public void Excluded_performance_bonus_uses_the_standard_amount_when_missing_workdays_are_at_most_one()
    {
        var actual = CalculateAmount(2_600_000m, 26m, 25m, "B", 0m, true);

        Assert.Equal(2_600_000m, actual);
    }

    [Fact]
    public void Excluded_performance_bonus_prorates_the_standard_amount_when_missing_workdays_exceed_one()
    {
        var actual = CalculateAmount(2_600_000m, 26m, 23m, "B", 0m, true);

        Assert.Equal(2_300_000m, actual);
    }

    [Theory]
    [InlineData("A", 900_000d)]
    [InlineData("B", 810_000d)]
    [InlineData("C", 720_000d)]
    public void Ratings_a_to_c_multiply_the_standard_amount_by_abc_and_performance_bonus(
        string abcRating,
        decimal expectedAmount)
    {
        var actual = CalculateAmount(1_000_000m, 26m, 23m, abcRating, 0.9m, false);

        Assert.Equal(expectedAmount, actual);
    }

    [Fact]
    public void Rating_d_uses_the_current_period_standard_workdays_when_bonus_is_included()
    {
        var actual = CalculateAmount(1_000_000m, 22m, 15m, "D", 0.8m, false);

        Assert.Equal(381_818.18m, actual);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    public void Non_positive_standard_amount_is_not_payable(decimal standardAmount, decimal expectedAmount)
    {
        Assert.Equal(expectedAmount, CalculateAmount(standardAmount, 26m, 26m, "A", 1m, false));
    }

    [Fact]
    public void Rating_d_rounds_to_two_decimal_places()
    {
        Assert.Equal(381_818.18m, CalculateAmount(1_000_000m, 22m, 15m, "D", 0.8m, false));
    }

    [Fact]
    public void Negative_late_or_early_minutes_do_not_create_negative_deduction()
    {
        var result = new ResponsibilityAllowanceWorkdayMetricsCalculator().Calculate(
            new ResponsibilityAllowanceWorkdayMetricsInput(
            [
                new(ResponsibilityAllowanceWorkdayEligibility.Eligible, -30m, -15m, new("HC"))
            ]));

        Assert.Equal(0m, result.LateEarlyDeductionDays);
        Assert.Equal(1m, result.AbcWorkdays);
    }

    [Fact]
    public void Unknown_rating_is_not_payable_and_zero_standard_days_are_safe()
    {
        Assert.Equal(0m, CalculateAmount(1_000_000m, 26m, 26m, "unexpected", 1m, false));
        Assert.Equal(0m, CalculateAmount(1_000_000m, 0m, 10m, "D", 1m, false));
    }

    private static decimal CalculateAmount(
        decimal standardAmount,
        decimal standardWorkdays,
        decimal actualWorkdays,
        string rating,
        decimal bonusFactor,
        bool excludeBonus) => new ResponsibilityAllowanceAmountCalculator().Calculate(
            new ResponsibilityAllowanceAmountInput(
                standardAmount,
                standardWorkdays,
                actualWorkdays,
                ResponsibilityAllowancePolicyStorageValues.ToAbcRating(rating),
                bonusFactor,
                excludeBonus
                    ? ResponsibilityAllowancePerformanceBonusApplication.Excluded
                    : ResponsibilityAllowancePerformanceBonusApplication.Applied)).ActualAmount;
}
