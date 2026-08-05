namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

public sealed record SetHazardAllowanceLockStateRequest(
    IReadOnlyCollection<Guid> PayrollAllowanceSummaryRecordIds,
    bool IsLocked,
    string RequestedBy);
