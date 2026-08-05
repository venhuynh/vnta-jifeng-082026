namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

public interface ILeaveHolidayAllowanceLockService
{
    Task<LeaveHolidayAllowanceListItemDto> SetLockStateAsync(SetLeaveHolidayAllowanceLockStateRequest request, CancellationToken cancellationToken = default);
    Task<SetLeaveHolidayAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetLeaveHolidayAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default);
}
