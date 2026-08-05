namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;

/// <summary>Pure deterministic calculation for one leave/holiday allowance record.</summary>
public static class LeaveHolidayAllowanceCalculationPolicy
{
    /// <summary>
    /// Preserves the persisted calculation sequence: normalize each source value to two decimals,
    /// then calculate and round the allowance amount away from zero.
    /// </summary>
    public static LeaveHolidayAllowanceCalculationResult Calculate(
        LeaveHolidayAllowanceCalculationInput input)
    {
        var dailyWageAmount = RoundToStoragePrecision(input.DailyWageAmount);
        var leaveDayCount = RoundToStoragePrecision(input.LeaveDayCount);
        var holidayDayCount = RoundToStoragePrecision(input.HolidayDayCount);

        return new LeaveHolidayAllowanceCalculationResult(
            dailyWageAmount,
            leaveDayCount,
            holidayDayCount,
            decimal.Round(
                dailyWageAmount * (leaveDayCount + holidayDayCount),
                2,
                MidpointRounding.AwayFromZero));
    }

    public static decimal RoundToStoragePrecision(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

}

/// <summary>Named source values used to calculate leave/holiday allowance.</summary>
public sealed record LeaveHolidayAllowanceCalculationInput(
    decimal DailyWageAmount,
    decimal LeaveDayCount,
    decimal HolidayDayCount);

/// <summary>Normalized values and calculated total that are persisted for an allowance record.</summary>
public sealed record LeaveHolidayAllowanceCalculationResult(
    decimal DailyWageAmount,
    decimal LeaveDayCount,
    decimal HolidayDayCount,
    decimal AllowanceAmount);
