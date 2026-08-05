namespace Vnta.Hrm.Application.QuanTri.TaiKhoanNhanVien;

public sealed record ResetEmployeeAccountPasswordRequest(
    Guid EmployeeId,
    string TemporaryPassword);
