using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;

/// <summary>Exports the server-authorized attendance-allowance snapshot for a payroll period.</summary>
public interface IAttendanceAllowanceExportService
{
    Task<IReadOnlyList<AttendanceAllowanceExportRowDto>> ExportAsync(
        AttendanceAllowanceExportRequest request,
        CancellationToken cancellationToken = default);
}
