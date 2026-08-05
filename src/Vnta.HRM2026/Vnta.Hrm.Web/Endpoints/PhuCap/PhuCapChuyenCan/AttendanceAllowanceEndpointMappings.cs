using Vnta.Hrm.Application.Common.Security;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapChuyenCan;

/// <summary>Registers the compatible attendance-allowance HTTP contract.</summary>
public static class AttendanceAllowanceEndpointMappings
{
    public static RouteGroupBuilder MapPhuCapChuyenCanEndpoints(this RouteGroupBuilder payrollGroup)
    {
        var group = payrollGroup
            .MapGroup("/attendance-allowance")
            .RequireAuthorization(InternalAccountPolicies.ManageAttendanceAllowance);

        group.MapGet("/rule", AttendanceAllowanceQueryEndpoints.GetRuleAsync);
        group.MapPost("/search", AttendanceAllowanceQueryEndpoints.SearchAsync);
        group.MapPost("/export", AttendanceAllowanceQueryEndpoints.ExportAsync);
        group.MapPost("/refresh", AttendanceAllowanceCommandEndpoints.RefreshAsync);
        group.MapPost("/actual-workday", AttendanceAllowanceCommandEndpoints.UpdateActualWorkdayAsync);
        group.MapPost("/standard-workday", AttendanceAllowanceCommandEndpoints.UpdateStandardWorkdayAsync);
        group.MapPost("/workdays", AttendanceAllowanceCommandEndpoints.UpdateWorkdaysAsync);
        group.MapPost("/lock-state", AttendanceAllowanceCommandEndpoints.SetLockStateAsync);
        group.MapPost("/lock-state/batch", AttendanceAllowanceCommandEndpoints.SetLockStateBatchAsync);
        return group;
    }
}
