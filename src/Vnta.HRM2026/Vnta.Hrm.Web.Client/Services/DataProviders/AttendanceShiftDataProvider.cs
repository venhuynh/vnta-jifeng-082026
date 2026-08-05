using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

public sealed class AttendanceShiftDataProvider(
    IAttendanceShiftService attendanceShiftService,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory)
{
    public Task<IReadOnlyList<AttendanceShiftRecord>> GetAsync(CancellationToken cancellationToken = default)
    {
        return GetFromServiceAsync(cancellationToken);
    }

    public Task<string?> ValidateAsync(
        AttendanceShiftRecord shift,
        CancellationToken cancellationToken = default)
    {
        shift.SyncWorkingDaysFromFlags();
        return attendanceShiftService.ValidateAsync(MapRequest(shift), cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceShiftRecord>> SaveAsync(
        AttendanceShiftRecord shift,
        bool isNew,
        CancellationToken cancellationToken = default)
    {
        shift.SyncWorkingDaysFromFlags();
        var request = MapRequest(shift);
        await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.Shift.Save,
            token => attendanceShiftService.SaveAsync(request, isNew, token),
            cancellationToken: cancellationToken);

        return await GetFromServiceAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<AttendanceShiftRecord>> GetFromServiceAsync(CancellationToken cancellationToken)
    {
        var rows = await attendanceShiftService.GetAsync(cancellationToken);
        return rows.Select(MapRecord).ToArray();
    }

    private static UpsertAttendanceShiftRequest MapRequest(AttendanceShiftRecord source) =>
        new()
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            ShortName = source.ShortName,
            Description = source.Description,
            DepartmentGroup = source.DepartmentGroup,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            IsOvernight = source.IsOvernight,
            BreakStartTime = source.BreakStartTime,
            BreakEndTime = source.BreakEndTime,
            Status = source.Status,
            ColorHex = source.ColorHex,
            WorkingDays = source.WorkingDays,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static AttendanceShiftRecord MapRecord(AttendanceShiftListItemDto source)
    {
        var record = new AttendanceShiftRecord
        {
            Id = source.Id,
            Code = source.Code,
            Name = source.Name,
            ShortName = source.ShortName,
            Description = source.Description,
            DepartmentGroup = source.DepartmentGroup,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            IsOvernight = source.IsOvernight,
            BreakStartTime = source.BreakStartTime,
            BreakEndTime = source.BreakEndTime,
            Status = source.Status,
            ColorHex = source.ColorHex,
            WorkingDays = source.WorkingDays,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

        record.SyncWorkingDayFlags();
        return record;
    }
}
