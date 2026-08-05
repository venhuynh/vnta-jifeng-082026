namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

public sealed record RefreshAttendanceAllowanceResult(
    int PayrollMonth,
    int PayrollYear,
    int MatchedRowCount,
    int UpdatedCount,
    int SkippedLockedCount,
    Guid? PayrollAllowanceSummaryRecordId = null);
