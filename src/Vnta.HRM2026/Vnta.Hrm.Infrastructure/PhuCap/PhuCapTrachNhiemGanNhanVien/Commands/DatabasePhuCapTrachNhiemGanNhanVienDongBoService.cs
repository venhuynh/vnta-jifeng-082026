using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>
/// Persistence boundary của đồng bộ gán nhân viên. Delegate về implementation đã có
/// để giữ nguyên tuyệt đối quy tắc assignment thủ công, dòng ABC khóa và tập Summary.
/// </summary>
public sealed class DatabasePhuCapTrachNhiemGanNhanVienDongBoService(
    IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService employeeAssignmentService)
    : IPhuCapTrachNhiemGanNhanVienDongBoService
{
    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> ExecuteAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        employeeAssignmentService.EnsureEmployeeAssignmentsForSummariesAsync(
            year,
            month,
            cancellationToken);
}
