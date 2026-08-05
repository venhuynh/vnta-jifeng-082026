namespace Vnta.Hrm.Application.DangTrienKhai.BangCongNgay;

public interface IAttendanceWorkdaySummaryService
{
    Task<RebuildAttendanceWorkdaySummaryResult> RebuildAsync(
        RebuildAttendanceWorkdaySummaryRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<AttendanceWorkdaySummaryListItemDto> UpdateAsync(
        UpdateAttendanceWorkdaySummaryRequest request,
        CancellationToken cancellationToken = default);

    Task SetLockStateAsync(
        SetAttendanceWorkdaySummaryLockStateRequest request,
        CancellationToken cancellationToken = default);
}
