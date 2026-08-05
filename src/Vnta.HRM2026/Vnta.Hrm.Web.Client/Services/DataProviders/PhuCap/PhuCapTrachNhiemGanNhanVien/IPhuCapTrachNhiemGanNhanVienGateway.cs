using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemGanNhanVien;

public interface IPhuCapTrachNhiemGanNhanVienGateway
{
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchAsync(PayrollResponsibilityAllowanceEmployeeAssignmentQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>> ExportAsync(PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest request, CancellationToken cancellationToken = default);
    Task<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult> UpdateAndRefreshAsync(UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> LoadFromPreviousMonthAsync(int year, int month, CancellationToken cancellationToken = default);
}
