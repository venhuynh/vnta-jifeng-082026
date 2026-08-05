using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>Owns row and period lock transitions for attendance allowance snapshots.</summary>
public interface IAttendanceAllowanceLockService
{
    Task<AttendanceAllowanceResultListItemDto> SetLockStateAsync(SetAttendanceAllowanceLockStateRequest request, CancellationToken cancellationToken = default);
    Task<SetAttendanceAllowanceBatchLockStateResult> SetLockStateBatchAsync(SetAttendanceAllowanceBatchLockStateRequest request, CancellationToken cancellationToken = default);
}
