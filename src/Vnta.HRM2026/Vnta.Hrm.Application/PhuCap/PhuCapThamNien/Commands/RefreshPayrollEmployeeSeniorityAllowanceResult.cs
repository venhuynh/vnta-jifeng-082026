namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public sealed record RefreshPayrollEmployeeSeniorityAllowanceResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount,
    int SkippedLockedCount);
