using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Contract read-only hẹp cho danh sách gán cấp bậc nhân viên.</summary>
public interface IPhuCapTrachNhiemGanNhanVienQueryService
{
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentQuery query,
        CancellationToken cancellationToken = default);
}
