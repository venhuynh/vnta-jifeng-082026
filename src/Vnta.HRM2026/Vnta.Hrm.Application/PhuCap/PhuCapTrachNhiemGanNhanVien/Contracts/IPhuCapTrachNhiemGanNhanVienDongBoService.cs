using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Contract hẹp cho việc đồng bộ tập gán cấp bậc từ Phụ cấp tổng hợp.</summary>
public interface IPhuCapTrachNhiemGanNhanVienDongBoService
{
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> ExecuteAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default);
}
