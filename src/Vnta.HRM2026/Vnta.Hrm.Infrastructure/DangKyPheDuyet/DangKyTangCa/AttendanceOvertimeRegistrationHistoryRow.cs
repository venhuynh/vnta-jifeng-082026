namespace Vnta.Hrm.Infrastructure.DangKyPheDuyet.DangKyTangCa;

public sealed class AttendanceOvertimeRegistrationHistoryRow
{
    public Guid Id { get; set; }

    public Guid RequestId { get; set; }

    public OvertimeRegistrationStatus? FromStatus { get; set; }

    public OvertimeRegistrationStatus ToStatus { get; set; }

    public string ActionName { get; set; } = string.Empty;

    public string? Note { get; set; }

    public Guid? PerformedByEmployeeId { get; set; }

    public string PerformedBy { get; set; } = string.Empty;

    public DateTime PerformedAtUtc { get; set; }
}
