namespace Vnta.Hrm.Application.NhanSu.PhongBan;

public sealed class AttendanceDepartmentDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CenterName { get; set; } = string.Empty;
    public string DepartmentOrWorkshopName { get; set; } = string.Empty;
    public string? TeamName { get; set; }
    public string? GroupName { get; set; }
    public string? Notes { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
