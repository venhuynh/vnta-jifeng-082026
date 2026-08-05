namespace Vnta.Hrm.Web.Client.Models.Payroll;

/// <summary>
/// Page đã được provider map sang view model UI.
/// </summary>
public sealed record AttendanceAllowanceResultLoadResult(
    IReadOnlyList<AttendanceAllowanceResultRecord> Rows,
    int TotalCount,
    int OpenCount,
    int LockedCount,
    int AttendanceClassACount,
    int AttendanceClassBCount,
    int AttendanceClassCCount,
    int PeriodTotalCount,
    int PeriodCanLockCount,
    int PeriodCanUnlockCount,
    int PeriodSummaryLockedCount);
