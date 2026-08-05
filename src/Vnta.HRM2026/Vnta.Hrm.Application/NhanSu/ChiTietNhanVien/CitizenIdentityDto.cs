namespace Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

public sealed record CitizenIdentityDto(
    Guid EmployeeId,
    bool HasCitizenIdentity,
    string? MaskedCitizenIdentityNumber,
    DateOnly? IssuedDate,
    string? IssuedPlace,
    DateOnly? ExpiryDate,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);
