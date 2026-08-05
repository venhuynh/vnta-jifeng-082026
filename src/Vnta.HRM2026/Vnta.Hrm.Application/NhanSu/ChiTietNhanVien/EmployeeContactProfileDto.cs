namespace Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

public sealed record EmployeeContactProfileDto(
    Guid EmployeeId,
    string? PersonalEmail,
    string? PersonalPhoneNumber,
    string? PermanentAddress,
    string? CurrentAddress,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhoneNumber,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
