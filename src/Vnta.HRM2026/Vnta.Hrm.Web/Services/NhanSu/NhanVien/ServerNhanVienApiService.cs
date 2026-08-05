using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Services.NhanSu.NhanVien;

public sealed class ServerNhanVienApiService(
    IEmployeeService employeeService,
    IEmployeeRefreshService employeeRefreshService)
    : IEmployeeApiService
{
    public Task<IReadOnlyList<EmployeeListItemDto>> SearchAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default) =>
        employeeService.SearchAsync(filter, cancellationToken);

    public Task<EmployeeSummaryDto> GetSummaryAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default) =>
        employeeService.GetSummaryAsync(filter, cancellationToken);

    public Task<EmployeeListItemDto> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default) =>
        employeeService.CreateAsync(request, cancellationToken);

    public Task<EmployeeListItemDto> UpdateAsync(
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default) =>
        employeeService.UpdateAsync(request, cancellationToken);

    public Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        employeeService.DeleteAsync(ids, cancellationToken);

    public Task<EmployeeRefreshResult> RefreshFromDeviceUserProfilesAsync(
        CancellationToken cancellationToken = default) =>
        employeeRefreshService.RefreshFromDeviceUserProfilesAsync(cancellationToken);
}
