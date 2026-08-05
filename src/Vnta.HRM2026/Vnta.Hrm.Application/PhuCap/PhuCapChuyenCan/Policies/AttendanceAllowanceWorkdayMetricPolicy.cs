namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;

/// <summary>Mức đủ điều kiện của một mã kết quả chấm công cho phụ cấp chuyên cần.</summary>
public enum AttendanceAllowanceWorkdayEligibility
{
    NotEligible = 0,
    Eligible = 1
}

/// <summary>Một ngày công đã được adapter đọc từ nguồn chấm công.</summary>
public sealed record AttendanceAllowanceWorkdayInput(
    int LateMinutes,
    int EarlyLeaveMinutes,
    string? AttendanceStatusCode,
    AttendanceAllowanceWorkdayEligibility Eligibility);

/// <summary>Các số liệu bảng công cần để tính phụ cấp chuyên cần.</summary>
public sealed record AttendanceAllowanceWorkdayMetric(
    decimal AdministrativeWorkdayCount,
    int LateEarlyMinutes,
    decimal LateEarlyDeductionDays,
    decimal AttendanceWorkdayCount,
    AttendanceAllowanceKpViolationState KpViolationState);

/// <summary>Chính sách tổng hợp số liệu phụ cấp từ các ngày công đã được đọc.</summary>
public sealed class AttendanceAllowanceWorkdayMetricPolicy
{
    public const int WorkdayDecimalPlaces = 4;
    public const int LateEarlyMinutesPerWorkday = 480;
    public const string KpAttendanceStatusCode = "KP";

    public AttendanceAllowanceWorkdayMetric Calculate(IEnumerable<AttendanceAllowanceWorkdayInput> workdays)
    {
        ArgumentNullException.ThrowIfNull(workdays);

        var administrativeWorkdayCount = 0m;
        var lateEarlyMinutes = 0;
        var kpViolationState = AttendanceAllowanceKpViolationState.NotPresent;

        foreach(var workday in workdays)
        {
            if(string.Equals(
                   NormalizeStatusCode(workday.AttendanceStatusCode),
                   KpAttendanceStatusCode,
                   StringComparison.OrdinalIgnoreCase))
            {
                kpViolationState = AttendanceAllowanceKpViolationState.Present;
            }

            lateEarlyMinutes += Math.Max(workday.LateMinutes, 0) + Math.Max(workday.EarlyLeaveMinutes, 0);
            if(workday.Eligibility == AttendanceAllowanceWorkdayEligibility.Eligible)
            {
                administrativeWorkdayCount += 1m;
            }
        }

        var lateEarlyDeductionDays = Math.Round(
            lateEarlyMinutes / (decimal)LateEarlyMinutesPerWorkday,
            WorkdayDecimalPlaces,
            MidpointRounding.AwayFromZero);
        var attendanceWorkdayCount = Math.Round(
            Math.Max(administrativeWorkdayCount - lateEarlyDeductionDays, 0m),
            WorkdayDecimalPlaces,
            MidpointRounding.AwayFromZero);

        return new AttendanceAllowanceWorkdayMetric(
            Math.Round(administrativeWorkdayCount, WorkdayDecimalPlaces, MidpointRounding.AwayFromZero),
            lateEarlyMinutes,
            lateEarlyDeductionDays,
            attendanceWorkdayCount,
            kpViolationState);
    }

    private static string? NormalizeStatusCode(string? statusCode) =>
        string.IsNullOrWhiteSpace(statusCode) ? null : statusCode.Trim().ToUpperInvariant();
}
