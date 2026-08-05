namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>Kết quả thay đổi trạng thái khóa theo lô trong một kỳ lương.</summary>
public sealed record SetPayrollAllowanceSummaryBatchLockStateResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount);
