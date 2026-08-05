namespace Vnta.Hrm.Infrastructure.NhanSu.PhongBan;

public sealed class AttendanceDepartmentRow
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CenterName { get; set; } = string.Empty;
    public string DepartmentOrWorkshopName { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string? GroupName { get; set; }
    public string? Notes { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
