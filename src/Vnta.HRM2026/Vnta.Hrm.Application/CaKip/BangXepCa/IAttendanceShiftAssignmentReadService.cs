namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public interface IAttendanceShiftAssignmentReadService
{
    Task<IReadOnlyList<AttendanceShiftAssignmentListItemDto>> SearchAsync(
        AttendanceShiftAssignmentFilter filter,
        CancellationToken cancellationToken = default);

    Task<AttendanceShiftRosterSnapshotDto> GetRosterAsync(
        AttendanceShiftRosterFilter filter,
        CancellationToken cancellationToken = default);
}
