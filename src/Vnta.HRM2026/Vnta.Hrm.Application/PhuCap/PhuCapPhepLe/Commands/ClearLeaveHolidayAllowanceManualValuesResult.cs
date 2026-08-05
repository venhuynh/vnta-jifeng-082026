namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record ClearLeaveHolidayAllowanceManualValuesResult(
    int RequestedCount,
    int ClearedCount,
    int SkippedLockedCount,
    int SkippedWithoutManualInputCount);
