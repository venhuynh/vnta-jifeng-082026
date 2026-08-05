namespace Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

public sealed record CreateChiTietNhanVienRequest(
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    Guid PositionId,
    int Status,
    DateTime? HireDate = null);
