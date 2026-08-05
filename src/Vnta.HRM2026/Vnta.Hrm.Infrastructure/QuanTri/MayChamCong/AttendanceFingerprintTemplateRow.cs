namespace Vnta.Hrm.Infrastructure.QuanTri.MayChamCong;

public sealed class AttendanceFingerprintTemplateRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string DeviceSn { get; set; } = string.Empty;

    public string Fid { get; set; } = string.Empty;

    public int? Size { get; set; }

    public string? Valid { get; set; }

    public string TemplateData { get; set; } = string.Empty;

    public string? MajorVersion { get; set; }

    public string? MinorVersion { get; set; }

    public string? Duress { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public AttendanceGatewayEmployeeRow? Employee { get; set; }
}
