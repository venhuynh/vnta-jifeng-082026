namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Policies;

/// <summary>Pure calculation policy for deriving allowance workdays from attendance entries.</summary>
public interface IOtherResponsibilityAllowanceWorkdayCalculator
{
    OtherResponsibilityAllowanceWorkdayCalculationResult Calculate(
        IReadOnlyCollection<OtherResponsibilityAllowanceAttendanceEntry> attendanceEntries);
}

/// <summary>Attendance eligibility as interpreted by this allowance calculation.</summary>
public enum OtherResponsibilityAllowanceWorkdayEligibility
{
    NotEligible = 0,
    EligibleAdministrativeWorkday = 1
}

/// <summary>One source attendance entry used when calculating an employee's allowance workdays.</summary>
public sealed record OtherResponsibilityAllowanceAttendanceEntry(
    DateOnly WorkDate,
    OtherResponsibilityAllowanceWorkdayEligibility Eligibility,
    decimal LateMinutes,
    decimal EarlyLeaveMinutes);

/// <summary>Rounded workday count used by the allowance amount policy.</summary>
public sealed record OtherResponsibilityAllowanceWorkdayCalculationResult(decimal AllowanceCalculationWorkdayCount);
