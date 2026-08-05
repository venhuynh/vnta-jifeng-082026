namespace Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

public sealed record ChiTietNhanVienDto(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    string? AvatarDataUrl,
    DateTime HireDate,
    Guid DepartmentId,
    string? DepartmentCode,
    string? DepartmentName,
    string? DepartmentPath,
    Guid PositionId,
    string? PositionCode,
    string? PositionName,
    int Status,
    DateTime? SeniorityStartDate,
    DateTime? ResignedDate,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
