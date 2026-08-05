using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>
/// Interactive Server adapter for the employee-assignment workflow.
/// WebAssembly replaces this registration with the HTTP gateway.
/// </summary>
public sealed class PhuCapTrachNhiemGanNhanVienGatewayAdapter(
    IPayrollResponsibilityAllowanceEmployeeAssignmentQueryService queryService,
    IPayrollResponsibilityAllowanceEmployeeAssignmentExportService exportService,
    IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService commandService)
    : IPhuCapTrachNhiemGanNhanVienGateway
{
    public Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentQuery query,
        CancellationToken cancellationToken = default) =>
        queryService.SearchEmployeeAssignmentsAsync(query, cancellationToken);

    public Task<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>> ExportAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest request,
        CancellationToken cancellationToken = default) =>
        exportService.ExportEmployeeAssignmentsAsync(request, cancellationToken);

    public Task<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult> UpdateAndRefreshAsync(
        UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest request,
        CancellationToken cancellationToken = default) =>
        commandService.UpdateAndRefreshEmployeeAssignmentAsync(request, cancellationToken);

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> LoadFromPreviousMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        return await commandService.LoadEmployeeAssignmentsFromPreviousMonthAsync(year, month, cancellationToken);
    }
}
