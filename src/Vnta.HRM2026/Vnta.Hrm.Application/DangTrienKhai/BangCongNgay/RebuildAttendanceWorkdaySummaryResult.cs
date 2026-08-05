namespace Vnta.Hrm.Application.DangTrienKhai.BangCongNgay;

public sealed record RebuildAttendanceWorkdaySummaryResult(
    DateOnly WorkDate,
    int RebuiltSummaryCount,
    int TotalPunchCount,
    decimal TotalWorkdayCredit,
    int UpdatedSummaryCount,
    int SkippedLockedCount);
