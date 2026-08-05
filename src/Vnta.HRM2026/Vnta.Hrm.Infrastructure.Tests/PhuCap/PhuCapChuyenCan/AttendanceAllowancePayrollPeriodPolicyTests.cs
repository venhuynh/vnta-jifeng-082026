using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.PhuCap.PhuCapChuyenCan;

public sealed class AttendanceAllowancePayrollPeriodPolicyTests
{
    [Theory]
    [InlineData(6, 2026, true)]
    [InlineData(5, 2026, false)]
    [InlineData(12, 2100, true)]
    [InlineData(1, 2101, false)]
    [InlineData(13, 2027, false)]
    public void IsSupported_uses_the_canonical_attendance_allowance_range(
        int payrollMonth,
        int payrollYear,
        bool expected)
    {
        Assert.Equal(expected, AttendanceAllowancePayrollPeriodPolicy.IsSupported(payrollMonth, payrollYear));
    }

    [Fact]
    public void Normalize_and_default_period_do_not_duplicate_the_minimum_supported_period()
    {
        Assert.Equal(
            new AttendanceAllowancePayrollPeriod(6, 2026),
            AttendanceAllowancePayrollPeriodPolicy.Normalize(1, 2025));
        Assert.Equal(
            new AttendanceAllowancePayrollPeriod(1, 2027),
            AttendanceAllowancePayrollPeriodPolicy.Normalize(0, 2027));
        Assert.Equal(
            new AttendanceAllowancePayrollPeriod(6, 2026),
            AttendanceAllowancePayrollPeriodPolicy.GetDefaultPayrollPeriod(
                new DateTimeOffset(2026, 1, 15, 8, 0, 0, TimeSpan.FromHours(7))));
    }

    [Fact]
    public void Request_validator_returns_the_canonical_period_error()
    {
        var validator = new AttendanceAllowanceRequestValidator();

        var result = validator.ValidatePeriod(5, 2026);

        Assert.False(result.IsValid);
        Assert.Equal(AttendanceAllowancePayrollPeriodPolicy.GetValidationError(5, 2026), result.ErrorMessage);
    }
}
