namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record UpsertAttendanceShiftAssignmentRequest(
    Guid EmployeeId,
    DateOnly WorkDate,
    Guid ShiftId,
    string? Source = null);
