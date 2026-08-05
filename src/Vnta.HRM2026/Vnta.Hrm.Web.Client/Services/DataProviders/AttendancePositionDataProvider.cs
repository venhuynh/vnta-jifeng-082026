using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class AttendancePositionDataProvider(IAttendancePositionService attendancePositionService)
{
    public Task<IReadOnlyList<AttendancePositionRecord>> GetAsync(CancellationToken cancellationToken = default)
    {
        return GetFromServiceAsync(cancellationToken);
    }

    public Task<string?> ValidateAsync(
        AttendancePositionRecord position,
        CancellationToken cancellationToken = default)
    {
        return attendancePositionService.ValidateAsync(MapRequest(position), cancellationToken);
    }

    public async Task<IReadOnlyList<AttendancePositionRecord>> SaveAsync(
        AttendancePositionRecord position,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        await attendancePositionService.SaveAsync(MapRequest(position), isNew, cancellationToken);
        return await GetFromServiceAsync(cancellationToken);
    }

    public Task RefreshEmployeeCountsAsync(CancellationToken cancellationToken = default) =>
        attendancePositionService.RefreshEmployeeCountsAsync(cancellationToken);

    private async Task<IReadOnlyList<AttendancePositionRecord>> GetFromServiceAsync(CancellationToken cancellationToken)
    {
        var rows = await attendancePositionService.GetAsync(cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    private static UpsertAttendancePositionRequest MapRequest(AttendancePositionRecord source) =>
        new()
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            Description = source.Description,
            Status = source.Status,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static AttendancePositionRecord MapRecord(AttendancePositionListItemDto source) =>
        new()
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            Description = source.Description,
            Status = source.Status,
            EmployeeCount = source.EmployeeCount,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
