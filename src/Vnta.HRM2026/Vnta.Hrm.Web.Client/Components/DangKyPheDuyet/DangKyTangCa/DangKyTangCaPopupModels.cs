using Vnta.Hrm.Application.DangKyPheDuyet.DangKyTangCa;

namespace Vnta.Hrm.Web.Client.Components.DangKyPheDuyet.DangKyTangCa;

public enum OvertimeRequestStatus
{
    Draft = 0,
    PendingApproval = 1,
    Returned = 2,
    Approved = 3,
    Rejected = 4
}

public enum OvertimeEmployeeAssignmentType
{
    None = 0,
    Until1900 = 1,
    Until2100 = 2,
    SpecialDayRegistered = 3
}

public sealed class OvertimeRequestEditModel
{
    public Guid Id { get; init; }

    public DateTime WorkDate { get; set; }

    public AttendanceWorkCalendarDayType DayType { get; set; }

    public string WorkshopCode { get; init; } = string.Empty;

    public string WorkshopName { get; init; } = string.Empty;

    public string RequestedBy { get; init; } = string.Empty;

    public string ApprovedBy { get; init; } = string.Empty;

    public OvertimeRequestStatus Status { get; set; }

    public string Note { get; set; } = string.Empty;

    public List<OvertimeEmployeeAssignmentRecord> EmployeeAssignments { get; set; } = [];

    public int TotalEmployeeCount => EmployeeAssignments.Count;

    public int RegisteredEmployeeCount => EmployeeAssignments.Count(employee => employee.AssignmentType != OvertimeEmployeeAssignmentType.None);

    public int Until1900Count => EmployeeAssignments.Count(employee => employee.AssignmentType == OvertimeEmployeeAssignmentType.Until1900);

    public int Until2100Count => EmployeeAssignments.Count(employee => employee.AssignmentType == OvertimeEmployeeAssignmentType.Until2100);

    public string RegistrationCutoffDisplay => AttendanceWorkCalendarDayTypes.IsSpecialDay(DayType)
        ? "Trước ngày làm thêm 1 ngày"
        : "Trước 15:00 cùng ngày";

    public string ApprovalCutoffDisplay => AttendanceWorkCalendarDayTypes.IsSpecialDay(DayType)
        ? "Duyệt toàn bộ trước ngày làm thêm"
        : "Trước 16:30 cùng ngày";

    public string StatusDisplay => DangKyTangCa.GetStatusDisplayName(Status);
}

public sealed class OvertimeEmployeeAssignmentRecord
{
    public Guid EmployeeId { get; init; }

    public string EmployeeCode { get; init; } = string.Empty;

    public string EmployeeName { get; init; } = string.Empty;

    public string PositionName { get; init; } = string.Empty;

    public string TeamCode { get; init; } = string.Empty;

    public string TeamName { get; init; } = string.Empty;

    public OvertimeEmployeeAssignmentType AssignmentType { get; set; }

    public string RegistrationHint { get; set; } = string.Empty;

    public string EmployeeDisplay => $"{EmployeeCode} - {EmployeeName}";

    public bool IsRegistered
    {
        get => AssignmentType != OvertimeEmployeeAssignmentType.None;
        set => AssignmentType = value
            ? OvertimeEmployeeAssignmentType.SpecialDayRegistered
            : OvertimeEmployeeAssignmentType.None;
    }

    public OvertimeEmployeeAssignmentRecord Clone() =>
        new()
        {
            EmployeeId = EmployeeId,
            EmployeeCode = EmployeeCode,
            EmployeeName = EmployeeName,
            PositionName = PositionName,
            TeamCode = TeamCode,
            TeamName = TeamName,
            AssignmentType = AssignmentType,
            RegistrationHint = RegistrationHint
        };
}
