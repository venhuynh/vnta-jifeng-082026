namespace Vnta.Hrm.Web.Client.Services.Api;

public interface IEmployeeApiService
{
    Task<IReadOnlyList<EmployeeListItemDto>> SearchAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default);

    Task<EmployeeSummaryDto> GetSummaryAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default);

    Task<EmployeeListItemDto> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task<EmployeeListItemDto> UpdateAsync(
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<EmployeeRefreshResult> RefreshFromDeviceUserProfilesAsync(
        CancellationToken cancellationToken = default);
}
