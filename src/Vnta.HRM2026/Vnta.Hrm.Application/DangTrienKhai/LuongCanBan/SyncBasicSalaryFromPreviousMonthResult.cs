namespace Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;

public sealed record SyncBasicSalaryFromPreviousMonthResult(
    int SourceMonth,
    int SourceYear,
    int TargetMonth,
    int TargetYear,
    int SourceRecordCount,
    int CreatedRecordCount,
    int UpdatedRecordCount,
    int UnchangedRecordCount,
    DateTime SynchronizedAtUtc);
