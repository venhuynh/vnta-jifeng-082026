namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public sealed record SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    IReadOnlyList<Guid>? PayrollAllowanceSummaryRecordIds = null);
