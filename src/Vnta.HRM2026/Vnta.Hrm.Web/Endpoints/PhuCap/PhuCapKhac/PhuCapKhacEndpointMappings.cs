namespace Vnta.Hrm.Web.Endpoints;

public static partial class PayrollEndpoints
{
    private static RouteGroupBuilder MapPhuCapKhacEndpoints(this RouteGroupBuilder payrollGroup)
    {
        // Keep the public routes unchanged while isolating this feature's HTTP
        // handlers and authorization boundary from the payroll composition root.
        var otherAllowanceGroup = payrollGroup
            .MapGroup("/other-allowances")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        otherAllowanceGroup.MapPost("/search", OtherAllowanceQueryEndpoints.SearchAsync);
        otherAllowanceGroup.MapPost("", OtherAllowanceCommandEndpoints.CreateAsync);
        otherAllowanceGroup.MapPost("/sync-previous-month", OtherAllowanceCommandEndpoints.SyncFromPreviousMonthAsync);
        otherAllowanceGroup.MapPut("", OtherAllowanceCommandEndpoints.UpdateAsync);
        otherAllowanceGroup.MapPost("/lock-state", OtherAllowanceCommandEndpoints.SetLockStateAsync);
        otherAllowanceGroup.MapPost("/lock-state/batch", OtherAllowanceCommandEndpoints.SetBatchLockStateAsync);
        otherAllowanceGroup.MapDelete("/{id:guid}", OtherAllowanceCommandEndpoints.DeleteAsync);
        return payrollGroup;
    }
}
