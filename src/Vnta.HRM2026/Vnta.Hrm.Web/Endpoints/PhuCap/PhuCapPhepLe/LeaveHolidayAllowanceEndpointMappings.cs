namespace Vnta.Hrm.Web.Endpoints;

public static partial class PayrollEndpoints
{
    private static RouteGroupBuilder MapLeaveHolidayAllowanceEndpoints(this RouteGroupBuilder payrollGroup)
    {
        // Keep authorization at this API boundary as well as on /api/payroll.
        var group = payrollGroup
            .MapGroup("/leave-holiday-allowance")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);
        group.MapPost("/prepare-period", LeaveHolidayAllowanceCommandEndpoints.PreparePeriodAsync);
        group.MapPost("/search", LeaveHolidayAllowanceQueryEndpoints.SearchAsync);
        group.MapPost("/clear-manual-values", LeaveHolidayAllowanceCommandEndpoints.ClearManualValuesAsync);
        group.MapPost("/sync-previous-month", LeaveHolidayAllowanceCommandEndpoints.SyncFromPreviousMonthAsync);
        group.MapPost("/recalculate", LeaveHolidayAllowanceCommandEndpoints.RecalculateAsync);
        group.MapPost("/manual-values", LeaveHolidayAllowanceCommandEndpoints.UpdateManualValuesAsync);
        group.MapPost("/lock-state", LeaveHolidayAllowanceCommandEndpoints.SetLockStateAsync);
        group.MapPost("/lock-state/batch", LeaveHolidayAllowanceCommandEndpoints.SetLockStateBatchAsync);
        return payrollGroup;
    }
}
