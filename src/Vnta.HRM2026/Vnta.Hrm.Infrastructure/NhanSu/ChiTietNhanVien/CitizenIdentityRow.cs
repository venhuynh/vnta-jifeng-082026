namespace Vnta.Hrm.Infrastructure.NhanSu.ChiTietNhanVien;

public sealed class CitizenIdentityRow
{
    public Guid EmployeeId { get; set; }
    public string CitizenIdentityNumberCiphertext { get; set; } = string.Empty;
    public string CitizenIdentityNumberHash { get; set; } = string.Empty;
    public DateOnly? IssuedDate { get; set; }
    public string? IssuedPlace { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
