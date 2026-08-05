namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftAssignmentEnsureRequest(
    DateOnly FromDate,
    DateOnly ToDate,
    string Source);
