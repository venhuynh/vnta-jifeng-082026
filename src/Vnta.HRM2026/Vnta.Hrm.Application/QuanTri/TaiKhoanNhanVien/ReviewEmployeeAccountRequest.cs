namespace Vnta.Hrm.Application.QuanTri.TaiKhoanNhanVien;

public sealed record ReviewEmployeeAccountRequest(
    Guid EmployeeId,
    string ReviewedByUserId,
    string? RejectionReason);
