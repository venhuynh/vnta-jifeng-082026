namespace Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

public sealed class UpsertOvertimeRegistrationRequest
{
    public Guid Id { get; set; }

    public DateOnly WorkDate { get; set; }

    public AttendanceWorkCalendarDayType DayType { get; set; }

    public string? Note { get; set; }

    public IReadOnlyList<UpsertOvertimeRegistrationEmployeeAssignmentRequest> EmployeeAssignments { get; set; } = [];
}

public sealed class UpsertOvertimeRegistrationEmployeeAssignmentRequest
{
    public Guid EmployeeId { get; set; }

    public OvertimeEmployeeAssignmentType AssignmentType { get; set; }
}
