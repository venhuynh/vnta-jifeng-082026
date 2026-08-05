namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

/// <summary>
/// Synchronizes the leave/holiday allowance data from the previous month.
/// </summary>
public interface ILeaveHolidayAllowancePreviousMonthSyncService
{
    Task<SyncLeaveHolidayAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncLeaveHolidayAllowanceFromPreviousMonthRequest request, CancellationToken cancellationToken = default);
}
