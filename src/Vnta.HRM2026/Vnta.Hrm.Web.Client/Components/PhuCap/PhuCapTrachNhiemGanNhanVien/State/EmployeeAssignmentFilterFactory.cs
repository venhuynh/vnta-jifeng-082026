using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemGanNhanVien.State;

/// <summary>Tạo truy vấn từ ảnh chụp trạng thái bộ lọc hiện tại.</summary>
internal interface IEmployeeAssignmentFilterFactory
{
    PayrollResponsibilityAllowanceEmployeeAssignmentQuery CreatePageQuery(EmployeeAssignmentReloadSnapshot snapshot);
}

internal sealed class EmployeeAssignmentFilterFactory : IEmployeeAssignmentFilterFactory
{
    public PayrollResponsibilityAllowanceEmployeeAssignmentQuery CreatePageQuery(EmployeeAssignmentReloadSnapshot snapshot) => new(
        snapshot.PayrollYear,
        snapshot.PayrollMonth,
        snapshot.SearchText,
        snapshot.GradePresenceKey,
        snapshot.PageIndex * snapshot.PageSize,
        snapshot.PageSize);
}
