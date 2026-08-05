namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public sealed record CreateEmployeeRequest(
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    Guid PositionId,
    int Status,
    DateTime? HireDate = null,
    string? Email = null,
    string? PhoneNumber = null);
