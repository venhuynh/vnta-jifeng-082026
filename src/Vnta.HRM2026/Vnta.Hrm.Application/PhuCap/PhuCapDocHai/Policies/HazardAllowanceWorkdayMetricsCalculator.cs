namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Named domain representation of one monthly attendance row used by hazard allowance.</summary>
public sealed record HazardAllowanceWorkday(
    string? DepartmentPath,
    decimal LateMinutes,
    decimal EarlyLeaveMinutes,
    bool QualifiesForHazardAllowance,
    string? PositionName = null);

/// <summary>Monthly attendance metrics used to calculate the allowance amount.</summary>
public sealed record HazardAllowanceWorkdayMetrics(
    string? DepartmentPath,
    decimal QualifiedWorkdayCount,
    decimal LateEarlyDeductionDays,
    string? PositionName = null);

public interface IHazardAllowanceWorkdayMetricsCalculator
{
    HazardAllowanceWorkdayMetrics Calculate(IEnumerable<HazardAllowanceWorkday> workdays);
}

/// <summary>
/// Aggregates attendance rows according to the hazard allowance rules.
/// Data loading and status-code translation remain outside this pure calculator.
/// </summary>
public sealed class HazardAllowanceWorkdayMetricsCalculator : IHazardAllowanceWorkdayMetricsCalculator
{
    public const decimal LateEarlyMinutesPerWorkday = 480m;

    public HazardAllowanceWorkdayMetrics Calculate(IEnumerable<HazardAllowanceWorkday> workdays)
    {
        ArgumentNullException.ThrowIfNull(workdays);

        var qualifiedWorkdays = 0m;
        var lateEarlyDeductionDays = 0m;
        string? departmentPath = null;
        string? positionName = null;

        foreach (var workday in workdays)
        {
            departmentPath ??= NormalizeOptional(workday.DepartmentPath);
            positionName ??= NormalizeOptional(workday.PositionName);
            if (workday.QualifiesForHazardAllowance) qualifiedWorkdays += 1m;
            var lateEarlyMinutes = Math.Max(workday.LateMinutes, 0m)
                + Math.Max(workday.EarlyLeaveMinutes, 0m);
            if (lateEarlyMinutes > 0m)
            {
                lateEarlyDeductionDays += lateEarlyMinutes / LateEarlyMinutesPerWorkday;
            }
        }

        return new HazardAllowanceWorkdayMetrics(
            departmentPath,
            decimal.Round(qualifiedWorkdays, 2, MidpointRounding.AwayFromZero),
            decimal.Round(lateEarlyDeductionDays, 4, MidpointRounding.AwayFromZero),
            positionName);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
