namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;

/// <summary>
/// Retains the existing non-authoritative client preview behavior. Persistence uses
/// <see cref="LeaveHolidayAllowanceCalculationPolicy"/>, which normalizes each input first.
/// </summary>
public static class LeaveHolidayAllowancePreviewPolicy
{
    public static LeaveHolidayAllowancePreviewCalculationResult Calculate(
        LeaveHolidayAllowancePreviewCalculationInput input) =>
        new(decimal.Round(
            input.DailyWageAmount * (input.LeaveDayCount + input.HolidayDayCount),
            2,
            MidpointRounding.AwayFromZero));
}

/// <summary>Named values shown by the legacy client-side allowance preview.</summary>
public sealed record LeaveHolidayAllowancePreviewCalculationInput(
    decimal DailyWageAmount,
    decimal LeaveDayCount,
    decimal HolidayDayCount);

/// <summary>Non-authoritative allowance amount shown before the server saves the record.</summary>
public sealed record LeaveHolidayAllowancePreviewCalculationResult(decimal AllowanceAmount);
