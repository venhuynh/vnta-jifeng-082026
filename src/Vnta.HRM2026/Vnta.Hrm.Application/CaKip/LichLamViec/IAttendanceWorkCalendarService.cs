namespace Vnta.Hrm.Application.CaKip.LichLamViec;

/// <summary>
/// Contract calendar dùng chung cho component Interactive Server và consumer HTTP; UI không phụ thuộc persistence.
/// </summary>
public interface IAttendanceWorkCalendarService
{
    Task<AttendanceWorkCalendarYearDto> GetYearAsync(
        int year,
        CancellationToken cancellationToken = default);

    Task<AttendanceWorkCalendarYearDto> EnsureSundayDayOffsAsync(
        int year,
        CancellationToken cancellationToken = default);

    Task<string?> ValidateAsync(
        UpsertAttendanceWorkCalendarDayRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendanceWorkCalendarDayDto> SaveAsync(
        UpsertAttendanceWorkCalendarDayRequest request,
        bool isNew,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
