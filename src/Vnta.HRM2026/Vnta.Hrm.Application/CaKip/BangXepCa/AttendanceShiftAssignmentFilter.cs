namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftAssignmentFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? EmployeeId = null,
    Guid? DepartmentId = null,
    Guid? ShiftId = null,
    string? CreationType = null,
    string? SearchText = null,
    int Take = 5000);
