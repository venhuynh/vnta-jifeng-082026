using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Queries;

#pragma warning disable CS0618 // This class is the explicit legacy compatibility facade.

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.DependencyInjection;

/// <summary>
/// Facade tương thích cho consumer cũ cần cả đọc và ghi.
/// Consumer mới phải inject contract hẹp để tránh coupling không cần thiết.
/// </summary>
[Obsolete("Compatibility facade; use capability-specific contracts instead. Remove after legacy consumers are retired.")]
public sealed class DatabaseLeaveHolidayAllowanceService(
    ILeaveHolidayAllowanceReadService readService,
    ILeaveHolidayAllowanceCommandService commandService)
    : ILeaveHolidayAllowanceService
{
    public Task<IReadOnlyList<LeaveHolidayAllowanceListItemDto>> SearchAsync(LeaveHolidayAllowanceFilter filter, CancellationToken cancellationToken = default) => readService.SearchAsync(filter, cancellationToken);
    public Task PreparePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default) => commandService.PreparePeriodAsync(payrollYear, payrollMonth, cancellationToken);
    public Task<ClearLeaveHolidayAllowanceManualValuesResult> ClearManualValuesAsync(ClearLeaveHolidayAllowanceManualValuesRequest request, CancellationToken cancellationToken = default) => commandService.ClearManualValuesAsync(request, cancellationToken);
    public Task<SyncLeaveHolidayAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncLeaveHolidayAllowanceFromPreviousMonthRequest request, CancellationToken cancellationToken = default) => commandService.SyncFromPreviousMonthAsync(request, cancellationToken);
    public Task<RecalculateLeaveHolidayAllowanceResult> RecalculateAsync(RecalculateLeaveHolidayAllowanceRequest request, CancellationToken cancellationToken = default) => commandService.RecalculateAsync(request, cancellationToken);
    public Task<LeaveHolidayAllowanceListItemDto> UpdateManualValuesAsync(UpdateLeaveHolidayAllowanceManualValuesRequest request, CancellationToken cancellationToken = default) => commandService.UpdateManualValuesAsync(request, cancellationToken);
    public Task<LeaveHolidayAllowanceListItemDto> SetLockStateAsync(SetLeaveHolidayAllowanceLockStateRequest request, CancellationToken cancellationToken = default) => commandService.SetLockStateAsync(request, cancellationToken);
    public Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetLeaveHolidayAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default) => commandService.SetLockStateBatchAsync(request, cancellationToken);
}
