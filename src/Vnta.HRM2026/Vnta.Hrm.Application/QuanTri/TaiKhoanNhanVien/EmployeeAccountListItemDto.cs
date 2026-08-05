namespace Vnta.Hrm.Application.QuanTri.TaiKhoanNhanVien;

public sealed record EmployeeAccountListItemDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? EmployeeEmail,
    string? DepartmentPath,
    string? PositionName,
    bool HasAccount,
    string? UserId,
    string? UserName,
    string? AccountEmail,
    string? ApprovalStatus,
    bool IsActive,
    string? AccessLevel,
    IReadOnlyList<string> RoleNames);
