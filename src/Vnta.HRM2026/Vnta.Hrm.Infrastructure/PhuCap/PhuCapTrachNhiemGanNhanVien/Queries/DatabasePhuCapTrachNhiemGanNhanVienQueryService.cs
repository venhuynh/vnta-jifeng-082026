using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Persistence boundary read-only của danh sách gán cấp bậc nhân viên.</summary>
public sealed class DatabasePhuCapTrachNhiemGanNhanVienQueryService(
    IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService employeeAssignmentQueryService)
    : IPhuCapTrachNhiemGanNhanVienQueryService
{
    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentQuery query,
        CancellationToken cancellationToken = default) =>
        employeeAssignmentQueryService.SearchEmployeeAssignmentsAsync(query, cancellationToken);
}
