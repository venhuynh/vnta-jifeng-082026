namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record RecalculateLeaveHolidayAllowanceResult(
    int PayrollMonth,
    int PayrollYear,
    int TotalRowCount,
    int UpdatedCount,
    int SkippedLockedCount);
