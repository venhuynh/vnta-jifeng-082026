using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Client.Audit;
using Vnta.Hrm.Web.Client.Models;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

// Adapter calendar cho UI; chỉ map DTO và giữ transport phía sau application interface.
public sealed class AttendanceWorkCalendarDataProvider(
    IAttendanceWorkCalendarService attendanceWorkCalendarService,
    IInteractiveAuditCommandScopeFactory auditCommandScopeFactory)
{
    public async Task<IReadOnlyList<AttendanceWorkCalendarDayRecord>> GetYearAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        // Component cache theo năm; provider luôn trả record UI độc lập với DTO Application.
        var result = await attendanceWorkCalendarService.GetYearAsync(year, cancellationToken);
        return result.Days.Select(MapRecord).ToArray();
    }

    public async Task<IReadOnlyList<AttendanceWorkCalendarDayRecord>> EnsureSundayDayOffsAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
        var result = await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.WorkCalendarDay.EnsureSundayDaysOff,
            token => attendanceWorkCalendarService.EnsureSundayDayOffsAsync(year, token),
            captureMode: AuditCaptureMode.OperationOnly,
            cancellationToken: cancellationToken);

        return result.Days.Select(MapRecord).ToArray();
    }

    public Task<string?> ValidateAsync(
        AttendanceWorkCalendarDayRecord record,
        CancellationToken cancellationToken = default)
    {
        return attendanceWorkCalendarService.ValidateAsync(MapRequest(record), cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceWorkCalendarDayRecord>> SaveAsync(
        AttendanceWorkCalendarDayRecord record,
        bool isNew,
        int year,
        CancellationToken cancellationToken = default)
    {
        var request = MapRequest(record);
        await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.WorkCalendarDay.Save,
            token => attendanceWorkCalendarService.SaveAsync(request, isNew, token),
            cancellationToken: cancellationToken);

        return await GetYearAsync(year, cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceWorkCalendarDayRecord>> DeleteAsync(
        Guid id,
        int year,
        CancellationToken cancellationToken = default)
    {
        await auditCommandScopeFactory.ExecuteAsync(
            AuditActions.WorkCalendarDay.Delete,
            token => attendanceWorkCalendarService.DeleteAsync(id, token),
            cancellationToken: cancellationToken);

        return await GetYearAsync(year, cancellationToken);
    }

    private static AttendanceWorkCalendarDayRecord MapRecord(AttendanceWorkCalendarDayDto source) =>
        new()
        {
            Id = source.Id,
            WorkDate = source.WorkDate.ToDateTime(TimeOnly.MinValue),
            DayType = source.DayType,
            Name = source.Name,
            Note = source.Note,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static UpsertAttendanceWorkCalendarDayRequest MapRequest(AttendanceWorkCalendarDayRecord source) =>
        new()
        {
            Id = source.Id,
            WorkDate = source.WorkDateOnly ?? default,
            DayType = source.DayType,
            Name = Normalize(source.Name),
            Note = Normalize(source.Note),
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
