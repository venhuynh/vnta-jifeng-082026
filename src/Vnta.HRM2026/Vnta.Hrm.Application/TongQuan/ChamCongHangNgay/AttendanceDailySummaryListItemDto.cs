namespace Vnta.Hrm.Application.TongQuan.ChamCongHangNgay;

public sealed record AttendanceDailySummaryListItemDto(
    Guid Id,
    Guid? EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? DepartmentName,
    string? PositionName,
    DateOnly WorkDate,
    int PunchCount,
    string ResultCode,
    string ResultText,
    string PunchMomentsText,
    DateTime? FirstPunchTime,
    DateTime? LastPunchTime,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
