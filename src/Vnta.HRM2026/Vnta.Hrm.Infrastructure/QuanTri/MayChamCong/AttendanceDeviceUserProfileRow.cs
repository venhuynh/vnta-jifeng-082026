namespace Vnta.Hrm.Infrastructure.QuanTri.MayChamCong;

public sealed class AttendanceDeviceUserProfileRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string DeviceSn { get; set; } = string.Empty;

    public string? FullName { get; set; }

    public string? Password { get; set; }

    public string? CardNumber { get; set; }

    public string? GroupCode { get; set; }

    public string? TimeZoneCode { get; set; }

    public string? PrivilegeCode { get; set; }

    public string? VerifyMode { get; set; }

    public string? ViceCard { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public AttendanceGatewayEmployeeRow? Employee { get; set; }
}
