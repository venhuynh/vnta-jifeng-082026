namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public interface IAttendanceShiftAssignmentManualEditService
{
    Task SaveManualAsync(
        UpsertAttendanceShiftAssignmentRequest request,
        CancellationToken cancellationToken = default);
}
