namespace Vnta.Hrm.Application.NhanSu.PhongBan;

public sealed class UpsertAttendanceDepartmentRequest
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string? CenterName { get; set; }
    public string? DepartmentOrWorkshopName { get; set; }
    public string? TeamName { get; set; }
    public string? GroupName { get; set; }
    public string? Notes { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
