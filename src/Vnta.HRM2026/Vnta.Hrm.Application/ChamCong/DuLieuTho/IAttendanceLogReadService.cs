namespace Vnta.Hrm.Application.ChamCong.DuLieuTho;

public interface IAttendanceLogReadService
{
    Task<IReadOnlyList<AttendanceLogListItemDto>> GetRecentAsync(
        int take = 500,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceLogListItemDto>> SearchAsync(
        AttendanceLogFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AttendanceLogListItemDto>> GetByDateRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        int take = 2000,
        CancellationToken cancellationToken = default);
}
