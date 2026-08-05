namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record SetLeaveHolidayAllowanceBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyList<Guid>? PayrollAllowanceSummaryRecordIds = null,
    string? Actor = null);
