namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

public sealed record RefreshHazardAllowanceResult(
    int PayrollMonth,
    int PayrollYear,
    int TotalSummaryRows,
    int CreatedCount,
    int UpdatedCount,
    int SkippedLockedCount,
    int IneligibleDepartmentCount,
    int ZeroWorkdayCount);
