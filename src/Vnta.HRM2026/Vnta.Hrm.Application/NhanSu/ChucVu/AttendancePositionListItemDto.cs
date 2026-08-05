namespace Vnta.Hrm.Application.NhanSu.ChucVu;

public sealed record AttendancePositionListItemDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    int Status,
    int EmployeeCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
