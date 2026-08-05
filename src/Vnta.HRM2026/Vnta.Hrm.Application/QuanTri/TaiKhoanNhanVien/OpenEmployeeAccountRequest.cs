namespace Vnta.Hrm.Application.QuanTri.TaiKhoanNhanVien;

public sealed record OpenEmployeeAccountRequest(
    Guid EmployeeId,
    string TemporaryPassword,
    string RoleName,
    string? AccessLevel);
