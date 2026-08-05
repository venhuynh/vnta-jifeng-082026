namespace Vnta.Hrm.Application.TongQuan.ChamCongHangNgay;

public interface IAttendanceDailySummaryReadService
{
    Task<IReadOnlyList<AttendanceDailySummaryListItemDto>> SearchAsync(
        AttendanceDailySummaryFilter filter,
        CancellationToken cancellationToken = default);
}
