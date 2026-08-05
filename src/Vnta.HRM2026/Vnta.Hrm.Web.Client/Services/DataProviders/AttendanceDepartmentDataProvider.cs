using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class AttendanceDepartmentDataProvider(IAttendanceDepartmentService attendanceDepartmentService)
{
    public Task<IReadOnlyList<AttendanceDepartmentRecord>> GetAsync(CancellationToken cancellationToken = default)
    {
        return GetFromServiceAsync(cancellationToken);
    }

    public Task<string?> ValidateAsync(
        AttendanceDepartmentRecord department,
        CancellationToken cancellationToken = default)
    {
        return attendanceDepartmentService.ValidateAsync(MapRequest(department), cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceDepartmentRecord>> SaveAsync(
        AttendanceDepartmentRecord department,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        await attendanceDepartmentService.SaveAsync(MapRequest(department), isNew, cancellationToken);
        return await GetFromServiceAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceDepartmentRecord>> DeleteAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        await attendanceDepartmentService.DeleteAsync(ids.ToArray(), cancellationToken);
        return await GetFromServiceAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AttendanceDepartmentRecord>> GetFromServiceAsync(CancellationToken cancellationToken)
    {
        var departments = await attendanceDepartmentService.GetAsync(cancellationToken);
        return departments
            .Select(MapRecord)
            .ToList();
    }

    private static UpsertAttendanceDepartmentRequest MapRequest(AttendanceDepartmentRecord source) =>
        new()
        {
            Id = source.Id,
            Code = source.Code,
            CenterName = source.CenterName,
            DepartmentOrWorkshopName = source.DepartmentOrWorkshopName,
            TeamName = source.TeamName,
            GroupName = source.GroupName,
            Notes = source.Notes,
            Status = source.Status,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static AttendanceDepartmentRecord MapRecord(AttendanceDepartmentDto source) =>
        new()
        {
            Id = source.Id,
            Code = source.Code,
            CenterName = source.CenterName,
            DepartmentOrWorkshopName = source.DepartmentOrWorkshopName,
            TeamName = source.TeamName,
            GroupName = source.GroupName,
            Notes = source.Notes,
            Name = source.Name,
            FullPath = source.FullPath,
            EmployeeCount = source.EmployeeCount,
            Status = source.Status,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
