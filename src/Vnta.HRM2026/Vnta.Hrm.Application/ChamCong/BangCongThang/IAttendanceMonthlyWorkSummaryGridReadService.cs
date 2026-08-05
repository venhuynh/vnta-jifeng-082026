namespace Vnta.Hrm.Application.ChamCong.BangCongThang;

/// <summary>
/// Contract đọc theo page nhân viên; implementation không được trả persistence entity cho UI.
/// </summary>
public interface IAttendanceMonthlyWorkSummaryGridReadService
{
    Task<AttendanceMonthlyWorkSummaryGridPageDto> SearchAsync(
        AttendanceMonthlyWorkSummaryGridFilter filter,
        CancellationToken cancellationToken = default);
}
