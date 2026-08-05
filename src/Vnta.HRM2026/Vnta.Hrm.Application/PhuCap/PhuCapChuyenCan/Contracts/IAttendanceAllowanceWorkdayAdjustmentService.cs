using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>
/// Executes the atomic manual workday adjustment use case for an attendance-allowance aggregate.
/// </summary>
public interface IAttendanceAllowanceWorkdayAdjustmentService
{
    Task<AttendanceAllowanceResultListItemDto> UpdateWorkdaysAsync(
        UpdateAttendanceAllowanceWorkdaysRequest request,
        CancellationToken cancellationToken = default);
}
