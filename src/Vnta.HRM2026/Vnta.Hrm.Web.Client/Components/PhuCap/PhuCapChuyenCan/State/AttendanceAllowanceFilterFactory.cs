using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;

/// <summary>
/// Builds the server query for the current attendance-allowance screen snapshot.
/// Query construction is kept outside the component/command handlers so the
/// reload workflow owns orchestration, while this policy owns filter shape.
/// </summary>
internal interface IAttendanceAllowanceFilterFactory
{
    AttendanceAllowanceResultFilter CreatePageFilter(AttendanceAllowanceReloadSnapshot snapshot);
}

internal sealed class AttendanceAllowanceFilterFactory : IAttendanceAllowanceFilterFactory
{
    public AttendanceAllowanceResultFilter CreatePageFilter(AttendanceAllowanceReloadSnapshot snapshot) =>
        new(
            PayrollAllowanceKind.Attendance,
            snapshot.PayrollMonth,
            snapshot.PayrollYear,
            snapshot.SearchText,
            Skip: snapshot.PageIndex * snapshot.PageSize,
            Take: snapshot.PageSize,
            LockState: snapshot.LockState,
            AttendanceClass: snapshot.AttendanceClass);
}
