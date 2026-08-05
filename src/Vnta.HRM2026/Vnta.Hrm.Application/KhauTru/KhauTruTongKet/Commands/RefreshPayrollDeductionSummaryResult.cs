namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

public sealed record RefreshPayrollDeductionSummaryResult(
    Guid SummaryRecordId,
    int PayrollYear,
    int PayrollMonth,
    int UpdatedCount,
    int UnchangedCount,
    int SkippedLockedCount,
    int MissingSourceCount);
