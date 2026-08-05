namespace Vnta.Hrm.Infrastructure.DangTrienKhai.DuLieuSinhTracHoc;

public sealed class AttendanceBioPhotoRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string DeviceSn { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public string? Type { get; set; }

    public int? Size { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public AttendanceGatewayEmployeeRow? Employee { get; set; }
}
