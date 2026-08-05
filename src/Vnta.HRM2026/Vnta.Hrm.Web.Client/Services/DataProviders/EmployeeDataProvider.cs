using Vnta.Hrm.Web.Client.Models.Employees;
using Vnta.Hrm.Web.Client.Services.Api;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class EmployeeDataProvider(
    IEmployeeApiService employeeApiService)
{
    public async Task<IReadOnlyList<EmployeeRecord>> GetAsync(CancellationToken cancellationToken = default)
    {
        var rows = await employeeApiService.SearchAsync(
            new EmployeeFilter(null),
            cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public async Task<IReadOnlyList<EmployeeRecord>> SearchAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        var rows = await employeeApiService.SearchAsync(filter, cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    public Task<EmployeeSummaryDto> GetSummaryAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default) =>
        employeeApiService.GetSummaryAsync(filter, cancellationToken);

    public async Task<EmployeeRecord> CreateAsync(
        CreateEmployeeFormModel model,
        CancellationToken cancellationToken = default)
    {
        var row = await employeeApiService.CreateAsync(
            new CreateEmployeeRequest(
                model.EmployeeCode ?? string.Empty,
                model.FullName ?? string.Empty,
                model.DepartmentId ?? Guid.Empty,
                model.PositionId ?? Guid.Empty,
                (int)model.Status,
                model.HireDate),
            cancellationToken);
        return MapRecord(row);
    }

    public async Task<EmployeeRecord> UpdateAsync(
        Guid employeeId,
        CreateEmployeeFormModel model,
        CancellationToken cancellationToken = default)
    {
        var row = await employeeApiService.UpdateAsync(
            new UpdateEmployeeRequest(
                employeeId,
                model.EmployeeCode ?? string.Empty,
                model.FullName ?? string.Empty,
                model.DepartmentId ?? Guid.Empty,
                model.PositionId ?? Guid.Empty,
                (int)model.Status,
                model.HireDate,
                model.OriginalUpdatedAtUtc),
            cancellationToken);
        return MapRecord(row);
    }

    public Task DeleteAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default) =>
        employeeApiService.DeleteAsync(
            ids.Where(id => id != Guid.Empty).Distinct().ToArray(),
            cancellationToken);

    public Task<EmployeeRefreshResult> RefreshFromDeviceUserProfilesAsync(CancellationToken cancellationToken = default) =>
        employeeApiService.RefreshFromDeviceUserProfilesAsync(cancellationToken);

    private static EmployeeRecord MapRecord(EmployeeListItemDto source) =>
        new()
        {
            Id = source.Id,
            EmployeeCode = source.EmployeeCode,
            FirstName = source.FirstName,
            LastName = source.LastName,
            Email = source.Email,
            PhoneNumber = source.PhoneNumber,
            AvatarDataUrl = source.AvatarDataUrl,
            HireDate = source.HireDate,
            DepartmentId = source.DepartmentId,
            DepartmentCode = source.DepartmentCode,
            DepartmentName = source.DepartmentName,
            DepartmentPath = source.DepartmentPath,
            PositionId = source.PositionId,
            PositionCode = source.PositionCode,
            PositionName = source.PositionName,
            Status = source.Status,
            SeniorityStartDate = source.SeniorityStartDate,
            ResignedDate = source.ResignedDate,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
