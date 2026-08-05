using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>UI boundary for employee-grade assignment list, export and mutations.</summary>
public sealed class PhuCapTrachNhiemGanNhanVienDataProvider(
    IPhuCapTrachNhiemGanNhanVienGateway gateway)
{
    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchAsync(PayrollResponsibilityAllowanceEmployeeAssignmentQuery query, CancellationToken cancellationToken = default) => gateway.SearchAsync(query, cancellationToken);
    public Task<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>> ExportAsync(PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest request, CancellationToken cancellationToken = default) => gateway.ExportAsync(request, cancellationToken);
    public Task<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult> UpdateAndRefreshAsync(UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest request, CancellationToken cancellationToken = default) => gateway.UpdateAndRefreshAsync(request, cancellationToken);
    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> LoadFromPreviousMonthAsync(int year, int month, CancellationToken cancellationToken = default) => gateway.LoadFromPreviousMonthAsync(year, month, cancellationToken);
}
