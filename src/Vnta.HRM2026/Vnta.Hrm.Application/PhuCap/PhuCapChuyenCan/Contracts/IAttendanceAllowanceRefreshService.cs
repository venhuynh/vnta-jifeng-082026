using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>Recalculates attendance-allowance snapshots for a payroll period or one summary row.</summary>
public interface IAttendanceAllowanceRefreshService
{
    Task<RefreshAttendanceAllowanceResult> RefreshAsync(
        RefreshAttendanceAllowanceRequest request,
        CancellationToken cancellationToken = default);
}
