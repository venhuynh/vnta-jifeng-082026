namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public sealed record UpdateEmployeeRequest(
    Guid Id,
    string EmployeeCode,
    string FullName,
    Guid DepartmentId,
    Guid PositionId,
    int Status,
    DateTime? HireDate = null,
    DateTime? OriginalUpdatedAtUtc = null,
    DateTime? SeniorityStartDate = null,
    DateTime? ResignedDate = null,
    bool UpdateEmploymentDates = false);
