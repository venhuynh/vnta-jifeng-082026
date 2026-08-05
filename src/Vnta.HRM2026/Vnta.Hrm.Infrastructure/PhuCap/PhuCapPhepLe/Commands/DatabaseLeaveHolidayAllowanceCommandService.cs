using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Policies;

#pragma warning disable CS0618 // Legacy command facade remains an intentional compatibility seam.

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Commands;

/// <summary>Compatibility-only command facade. Persistence belongs to the individual use-case services.</summary>
[Obsolete("Compatibility command facade; use capability-specific contracts instead. Remove after compatibility consumers are retired.")]
public sealed class DatabaseLeaveHolidayAllowanceCommandService(
    ILeaveHolidayAllowancePeriodPreparationService periodPreparation,
    ILeaveHolidayAllowanceClearManualValuesService clearManualValues,
    ILeaveHolidayAllowancePreviousMonthSyncService previousMonthSync,
    ILeaveHolidayAllowanceRecalculationService recalculation,
    ILeaveHolidayAllowanceManualAdjustmentService manualAdjustment,
    ILeaveHolidayAllowanceLockService lockService)
    : ILeaveHolidayAllowanceCommandService
{
    // Retains the focused-persistence-test construction seam without reintroducing a command core.
    public DatabaseLeaveHolidayAllowanceCommandService(ApplicationDbContext dbContext)
        : this(
            new DatabaseLeaveHolidayAllowancePeriodPreparationService(dbContext, new LeaveHolidayAllowanceRequestValidator()),
            new DatabaseLeaveHolidayAllowanceClearManualValuesService(dbContext, new LeaveHolidayAllowanceRequestValidator()),
            new DatabaseLeaveHolidayAllowancePreviousMonthSyncService(dbContext, new LeaveHolidayAllowanceRequestValidator()),
            new DatabaseLeaveHolidayAllowanceRecalculationService(dbContext, new DatabaseLeaveHolidayAllowanceRecalculationSource(dbContext), new LeaveHolidayAllowanceRequestValidator()),
            new DatabaseLeaveHolidayAllowanceManualAdjustmentService(dbContext, new LeaveHolidayAllowanceRequestValidator()),
            new DatabaseLeaveHolidayAllowanceLockService(dbContext, new LeaveHolidayAllowanceRequestValidator())) { }

    public Task PreparePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default) => periodPreparation.PreparePeriodAsync(payrollYear, payrollMonth, cancellationToken);
    public Task<ClearLeaveHolidayAllowanceManualValuesResult> ClearManualValuesAsync(ClearLeaveHolidayAllowanceManualValuesRequest request, CancellationToken cancellationToken = default) => clearManualValues.ClearManualValuesAsync(request, cancellationToken);
    public Task<SyncLeaveHolidayAllowanceFromPreviousMonthResult> SyncFromPreviousMonthAsync(SyncLeaveHolidayAllowanceFromPreviousMonthRequest request, CancellationToken cancellationToken = default) => previousMonthSync.SyncFromPreviousMonthAsync(request, cancellationToken);
    public Task<RecalculateLeaveHolidayAllowanceResult> RecalculateAsync(RecalculateLeaveHolidayAllowanceRequest request, CancellationToken cancellationToken = default) => recalculation.RecalculateAsync(request, cancellationToken);
    public Task<LeaveHolidayAllowanceListItemDto> UpdateManualValuesAsync(UpdateLeaveHolidayAllowanceManualValuesRequest request, CancellationToken cancellationToken = default) => manualAdjustment.UpdateManualValuesAsync(request, cancellationToken);
    public Task<LeaveHolidayAllowanceListItemDto> SetLockStateAsync(SetLeaveHolidayAllowanceLockStateRequest request, CancellationToken cancellationToken = default) => lockService.SetLockStateAsync(request, cancellationToken);
    public Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetLeaveHolidayAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default) => lockService.SetLockStateBatchAsync(request, cancellationToken);
}
