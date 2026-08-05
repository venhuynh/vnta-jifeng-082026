namespace Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

public sealed record UpdateChiTietNhanVienRequest(
    Guid Id,
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    Guid PositionId,
    int Status,
    DateTime? HireDate = null,
    DateTime? OriginalUpdatedAtUtc = null,
    DateTime? SeniorityStartDate = null,
    DateTime? ResignedDate = null);
