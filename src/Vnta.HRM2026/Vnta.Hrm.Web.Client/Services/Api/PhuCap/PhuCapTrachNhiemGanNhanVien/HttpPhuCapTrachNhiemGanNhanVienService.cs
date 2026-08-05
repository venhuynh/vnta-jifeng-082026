using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTrachNhiemGanNhanVien;

namespace Vnta.Hrm.Web.Client.Services.Api.PhuCap.PhuCapTrachNhiemGanNhanVien;

/// <summary>Typed HTTP client containing only employee-grade assignment use cases.</summary>
public sealed class HttpPhuCapTrachNhiemGanNhanVienService(NavigationManager navigationManager)
    : IPhuCapTrachNhiemGanNhanVienGateway
{
    private readonly HttpClient httpClient = new() { BaseAddress = new Uri(navigationManager.BaseUri) };

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto> SearchAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/search", query, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentPageDto>(cancellationToken);
    }

    public async Task<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>> ExportAsync(
        PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/export", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<IReadOnlyList<PayrollResponsibilityAllowanceEmployeeAssignmentExportItemDto>>(cancellationToken);
    }

    public async Task<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult> UpdateAndRefreshAsync(
        UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/update-and-refresh", request, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<UpdatePayrollResponsibilityAllowanceEmployeeAssignmentResult>(cancellationToken);
    }

    public async Task<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult> LoadFromPreviousMonthAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            "api/payroll/responsibility-allowance/employee-assignments/load-from-previous-month", new { year, month }, cancellationToken);
        return await response.ReadRequiredFromJsonAsync<PayrollResponsibilityAllowanceEmployeeAssignmentBulkResult>(cancellationToken);
    }

}
