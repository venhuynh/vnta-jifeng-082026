namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Policies;

/// <summary>
/// Supplies the externally-owned attendance and basic-salary facts used by recalculation.
/// Implementations belong to infrastructure; calculation and command semantics do not.
/// </summary>
public interface ILeaveHolidayAllowanceRecalculationSource
{
    Task<IReadOnlyDictionary<Guid, LeaveHolidayAllowanceRecalculationSourceValues>>
        GetSourceValuesAsync(
            LeaveHolidayAllowanceRecalculationSourceRequest request,
            CancellationToken cancellationToken = default);
}

public sealed record LeaveHolidayAllowanceRecalculationSourceRequest(
    int PayrollMonth,
    int PayrollYear,
    IReadOnlyCollection<Guid> EmployeeIds);

public sealed record LeaveHolidayAllowanceRecalculationSourceValues(
    decimal? DailyWageAmount,
    decimal LeaveDayCount);
