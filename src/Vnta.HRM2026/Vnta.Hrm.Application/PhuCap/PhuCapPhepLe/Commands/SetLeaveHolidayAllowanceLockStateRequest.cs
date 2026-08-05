namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public sealed record SetLeaveHolidayAllowanceLockStateRequest(
    Guid PayrollAllowanceSummaryRecordId,
    bool IsLocked,
    string? Actor = null,
    DateTime? OriginalUpdatedAtUtc = null);
