using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>
/// Compatibility contract for clients that update one workday value at a time.
/// New workflows that edit both values must use <see cref="IAttendanceAllowanceWorkdayAdjustmentService"/>
/// so the pair is persisted in one transaction and with one optimistic-concurrency version.
/// </summary>
public interface IAttendanceAllowanceManualAdjustmentService
{
    Task<AttendanceAllowanceResultListItemDto> UpdateActualWorkdayAsync(UpdateAttendanceAllowanceActualWorkdayRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceAllowanceResultListItemDto> UpdateStandardWorkdayAsync(UpdateAttendanceAllowanceStandardWorkdayRequest request, CancellationToken cancellationToken = default);
}
