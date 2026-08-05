namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;

public sealed record SetMealAllowanceLockStateBatchResult(
    int PayrollYear,
    int PayrollMonth,
    int TargetRowCount,
    int UpdatedCount);
