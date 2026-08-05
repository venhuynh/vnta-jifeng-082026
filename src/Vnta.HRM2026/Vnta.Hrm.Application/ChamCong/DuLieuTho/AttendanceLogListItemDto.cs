namespace Vnta.Hrm.Application.ChamCong.DuLieuTho;

public sealed record AttendanceLogListItemDto(
    Guid Id,
    Guid DeviceId,
    Guid? EmployeeId,
    string? DeviceCode,
    string? DeviceName,
    string? EmployeeCode,
    string? EmployeeName,
    DateTime? AttTime,
    string? Status,
    string? Verify,
    string? WorkCode,
    string? Reserved1,
    string? Reserved2,
    int? MaskFlag,
    string? Temperature,
    string DedupKey,
    DateTime UpdateTime,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
