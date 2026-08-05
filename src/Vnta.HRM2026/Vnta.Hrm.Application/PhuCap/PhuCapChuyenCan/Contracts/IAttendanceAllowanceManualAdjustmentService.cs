using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>Owns the two manual workday adjustments for an unlocked attendance allowance row.</summary>
public interface IAttendanceAllowanceManualAdjustmentService
{
    Task<AttendanceAllowanceResultListItemDto> UpdateActualWorkdayAsync(UpdateAttendanceAllowanceActualWorkdayRequest request, CancellationToken cancellationToken = default);
    Task<AttendanceAllowanceResultListItemDto> UpdateStandardWorkdayAsync(UpdateAttendanceAllowanceStandardWorkdayRequest request, CancellationToken cancellationToken = default);
}
