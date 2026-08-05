namespace Vnta.Hrm.Application.TongQuan.ChamCongHangNgay;

public interface IAttendanceDailySummaryService
{
    Task<RebuildAttendanceDailySummaryResult> RebuildAsync(
        RebuildAttendanceDailySummaryRequest request,
        CancellationToken cancellationToken = default);
}
