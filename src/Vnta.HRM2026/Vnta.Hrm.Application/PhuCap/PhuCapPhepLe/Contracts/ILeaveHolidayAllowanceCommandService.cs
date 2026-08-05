namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

/// <summary>
/// Tập hợp command làm thay đổi snapshot Phụ cấp Phép - Lễ.
/// Authorization, audit và concurrency được thực thi ở biên server.
/// </summary>
[Obsolete("Use the capability-specific contracts (period preparation, recalculation, manual adjustment and lock) instead. Remove after compatibility consumers are retired.")]
public interface ILeaveHolidayAllowanceCommandService
{
    Task PreparePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default);

    Task<ClearLeaveHolidayAllowanceManualValuesResult> ClearManualValuesAsync(
        ClearLeaveHolidayAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default);

    Task<SyncLeaveHolidayAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        SyncLeaveHolidayAllowanceFromPreviousMonthRequest request,
        CancellationToken cancellationToken = default);

    Task<RecalculateLeaveHolidayAllowanceResult> RecalculateAsync(
        RecalculateLeaveHolidayAllowanceRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaveHolidayAllowanceListItemDto> UpdateManualValuesAsync(
        UpdateLeaveHolidayAllowanceManualValuesRequest request,
        CancellationToken cancellationToken = default);

    Task<LeaveHolidayAllowanceListItemDto> SetLockStateAsync(
        SetLeaveHolidayAllowanceLockStateRequest request,
        CancellationToken cancellationToken = default);

    Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(
        SetLeaveHolidayAllowanceBatchLockStateRequest request,
        CancellationToken cancellationToken = default);
}
