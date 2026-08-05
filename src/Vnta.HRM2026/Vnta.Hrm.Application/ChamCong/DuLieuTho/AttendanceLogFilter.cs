namespace Vnta.Hrm.Application.ChamCong.DuLieuTho;

public sealed record AttendanceLogFilter(
    string? SearchTerm,
    DateTime? FromDate,
    DateTime? ToDate,
    Guid? EmployeeId = null,
    int Take = 2000);
