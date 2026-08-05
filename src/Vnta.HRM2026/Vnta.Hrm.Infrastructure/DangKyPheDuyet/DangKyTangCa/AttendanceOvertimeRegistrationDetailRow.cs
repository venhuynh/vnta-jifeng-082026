namespace Vnta.Hrm.Infrastructure.DangKyPheDuyet.DangKyTangCa;

public sealed class AttendanceOvertimeRegistrationDetailRow
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public Guid EmployeeId { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;

    public string EmployeeName { get; set; } = string.Empty;

    public string PositionName { get; set; } = string.Empty;

    public string TeamCode { get; set; } = string.Empty;

    public string TeamName { get; set; } = string.Empty;

    public OvertimeEmployeeAssignmentType AssignmentType { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
