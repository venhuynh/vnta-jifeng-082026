namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public sealed record SetPayrollEmployeeSeniorityAllowanceLockStateRequest(
    Guid PayrollAllowanceSummaryRecordId,
    bool IsLocked,
    DateTime OriginalUpdatedAtUtc);
