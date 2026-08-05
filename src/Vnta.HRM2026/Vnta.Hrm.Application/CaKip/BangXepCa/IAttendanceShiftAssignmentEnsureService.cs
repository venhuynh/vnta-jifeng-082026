namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public interface IAttendanceShiftAssignmentEnsureService
{
    Task<AttendanceShiftAssignmentEnsureResult> EnsureFromSchedulingSettingsAsync(
        AttendanceShiftAssignmentEnsureRequest request,
        CancellationToken cancellationToken = default);
}
