namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;

/// <summary>One normalized payroll period supported by the attendance-allowance feature.</summary>
public readonly record struct AttendanceAllowancePayrollPeriod(int Month, int Year);

/// <summary>
/// Canonical supported-period policy for attendance allowance. UI, request validation and persistence
/// adapters use this policy instead of maintaining independent payroll-period boundaries.
/// </summary>
public static class AttendanceAllowancePayrollPeriodPolicy
{
    public const int MinimumSupportedMonth = 6;
    public const int MinimumSupportedYear = 2026;
    public const int MaximumSupportedYear = 2100;

    public static AttendanceAllowancePayrollPeriod MinimumSupportedPayrollPeriod { get; } =
        new(MinimumSupportedMonth, MinimumSupportedYear);

    /// <summary>Returns the feature-default payroll period for the supplied local time.</summary>
    public static AttendanceAllowancePayrollPeriod GetDefaultPayrollPeriod(DateTimeOffset currentLocalTime) =>
        Normalize(currentLocalTime.Month, currentLocalTime.Year);

    /// <summary>Clamps a candidate period into the period range supported by this feature.</summary>
    public static AttendanceAllowancePayrollPeriod Normalize(int payrollMonth, int payrollYear)
    {
        var normalizedMonth = Math.Clamp(payrollMonth, 1, 12);
        var normalizedYear = Math.Clamp(payrollYear, MinimumSupportedYear, MaximumSupportedYear);

        return normalizedYear == MinimumSupportedYear && normalizedMonth < MinimumSupportedMonth
            ? MinimumSupportedPayrollPeriod
            : new(normalizedMonth, normalizedYear);
    }

    /// <summary>Determines whether a candidate period is supported without allocating a validation result.</summary>
    public static bool IsSupported(int payrollMonth, int payrollYear) =>
        GetValidationError(payrollMonth, payrollYear) is null;

    /// <summary>Returns a user-safe validation error for an unsupported payroll period, if any.</summary>
    public static string? GetValidationError(int payrollMonth, int payrollYear)
    {
        if(payrollYear is < MinimumSupportedYear or > MaximumSupportedYear)
            return $"Năm kỳ lương phải nằm trong khoảng từ {MinimumSupportedYear} đến {MaximumSupportedYear}.";

        if(payrollMonth is < 1 or > 12)
            return "Tháng kỳ lương phải nằm trong khoảng từ 1 đến 12.";

        return payrollYear == MinimumSupportedYear && payrollMonth < MinimumSupportedMonth
            ? $"Dữ liệu phụ cấp chuyên cần bắt đầu từ {MinimumSupportedMonth:00}/{MinimumSupportedYear}."
            : null;
    }

    /// <summary>Throws when the candidate period falls outside this feature's supported range.</summary>
    public static void EnsureSupported(int payrollMonth, int payrollYear)
    {
        var errorMessage = GetValidationError(payrollMonth, payrollYear);
        if(errorMessage is not null)
            throw new InvalidOperationException(errorMessage);
    }
}
