namespace Vnta.Hrm.Application.DangTrienKhai.DuLieuSinhTracHoc;

public sealed record AttendanceBiometricDataListItemDto(
    Guid Id,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    string? AvatarDataUrl,
    string? DepartmentName,
    string? PositionName,
    int FpQty,
    bool HasFaceData,
    DateTime LastUpdated,
    string? CardNumber,
    bool IsAdmin,
    bool HasPassword);
