namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Policies;

public sealed class OtherResponsibilityAllowanceWorkdayCalculator
    : IOtherResponsibilityAllowanceWorkdayCalculator
{
    private const decimal FullAdministrativeWorkdayMinutes = 480m;

    public OtherResponsibilityAllowanceWorkdayCalculationResult Calculate(
        IReadOnlyCollection<OtherResponsibilityAllowanceAttendanceEntry> attendanceEntries)
    {
        ArgumentNullException.ThrowIfNull(attendanceEntries);

        var calculationWorkdays = attendanceEntries
            .GroupBy(entry => entry.WorkDate)
            .Sum(CalculateWorkdayForDate);

        return new OtherResponsibilityAllowanceWorkdayCalculationResult(RoundWorkdays(calculationWorkdays));
    }

    private static decimal CalculateWorkdayForDate(
        IGrouping<DateOnly, OtherResponsibilityAllowanceAttendanceEntry> entriesForDate)
    {
        if (!entriesForDate.Any(entry => entry.Eligibility == OtherResponsibilityAllowanceWorkdayEligibility.EligibleAdministrativeWorkday))
        {
            return 0m;
        }

        var adjustedMinutes = Math.Min(
            entriesForDate.Sum(entry => Math.Max(entry.LateMinutes, 0m) + Math.Max(entry.EarlyLeaveMinutes, 0m)),
            FullAdministrativeWorkdayMinutes);
        return Math.Max(1m - adjustedMinutes / FullAdministrativeWorkdayMinutes, 0m);
    }

    private static decimal RoundWorkdays(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
