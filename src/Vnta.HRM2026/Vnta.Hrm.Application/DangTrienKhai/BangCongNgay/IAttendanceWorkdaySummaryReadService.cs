namespace Vnta.Hrm.Application.DangTrienKhai.BangCongNgay;

public interface IAttendanceWorkdaySummaryReadService
{
    Task<IReadOnlyList<AttendanceWorkdaySummaryListItemDto>> SearchAsync(
        AttendanceWorkdaySummaryFilter filter,
        CancellationToken cancellationToken = default);
}
