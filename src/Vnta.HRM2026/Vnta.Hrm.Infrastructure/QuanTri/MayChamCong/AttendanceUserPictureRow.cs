namespace Vnta.Hrm.Infrastructure.QuanTri.MayChamCong;

public sealed class AttendanceUserPictureRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string DeviceSn { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public int? Size { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public AttendanceGatewayEmployeeRow? Employee { get; set; }
}
