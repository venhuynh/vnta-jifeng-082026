namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;

public sealed record SetOtherResponsibilityAllowanceBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount);
