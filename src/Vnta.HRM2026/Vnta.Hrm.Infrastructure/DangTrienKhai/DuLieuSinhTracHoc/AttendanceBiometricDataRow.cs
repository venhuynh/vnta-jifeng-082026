namespace Vnta.Hrm.Infrastructure.DangTrienKhai.DuLieuSinhTracHoc;

public sealed class AttendanceBiometricDataRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public int FpQty { get; set; }

    public bool HasFaceData { get; set; }

    public DateTime LastUpdated { get; set; }

    public string? CardNumber { get; set; }

    public bool IsAdmin { get; set; }

    public string? Password { get; set; }

    public AttendanceGatewayEmployeeRow? Employee { get; set; }
}
