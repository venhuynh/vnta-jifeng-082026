namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftRosterFilter(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? DepartmentId = null,
    Guid? EmployeeId = null,
    string? SearchText = null,
    bool IncludeInactiveEmployees = false);
