namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;

/// <summary>Changes the lock state of detail snapshots for one payroll period.</summary>
public sealed record SetOtherResponsibilityAllowanceBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyList<Guid>? PayrollAllowanceSummaryRecordIds,
    IReadOnlyList<OtherResponsibilityAllowanceLockStateConcurrencyToken>? ConcurrencyTokens);

public sealed record OtherResponsibilityAllowanceLockStateConcurrencyToken(
    Guid PayrollAllowanceSummaryRecordId,
    DateTime? OriginalUpdatedAtUtc);
