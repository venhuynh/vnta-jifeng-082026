namespace Vnta.Hrm.Application.CaKip.BangXepCa;

public sealed record AttendanceShiftAssignmentEnsureResult(
    DateOnly FromDate,
    DateOnly ToDate,
    int DateCount,
    int EligibleEmployeeCount,
    int InsertedCount,
    int UpdatedCount,
    int UnchangedCount,
    int ProtectedCount,
    IReadOnlyList<AttendanceShiftAssignmentEnsureIssueDto> Issues,
    int SkippedNonWorkingDateCount = 0,
    int DeletedNonWorkingAutoRuleCount = 0);
