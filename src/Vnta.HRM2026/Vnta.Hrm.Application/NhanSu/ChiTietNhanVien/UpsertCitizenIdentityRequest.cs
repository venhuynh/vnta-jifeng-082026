namespace Vnta.Hrm.Application.NhanSu.ChiTietNhanVien;

public sealed record UpsertCitizenIdentityRequest(
    Guid EmployeeId,
    string? CitizenIdentityNumber,
    DateOnly? IssuedDate,
    string? IssuedPlace,
    DateOnly? ExpiryDate,
    DateTime? OriginalUpdatedAtUtc = null);
