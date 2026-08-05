namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Commands;

public enum MealAllowanceLockActionScope
{
    SelectedRows = 1,
    WholePeriod = 2
}

public sealed record SetMealAllowanceLockStateBatchRequest(
    int PayrollYear,
    int PayrollMonth,
    bool IsLocked,
    MealAllowanceLockActionScope Scope,
    IReadOnlyList<Guid>? RecordIds = null,
    string? Actor = null);
