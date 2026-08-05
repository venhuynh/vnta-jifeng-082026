namespace Vnta.Hrm.Web.Endpoints;

public static partial class PayrollEndpoints
{
    private static RouteGroupBuilder MapPhuCapComEndpoints(this RouteGroupBuilder payrollGroup)
    {
        // Module này chỉ chịu trách nhiệm HTTP boundary; policy được áp dụng cho payrollGroup tại composition root.
        var group = payrollGroup
            .MapGroup("/meal-allowance")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        group.MapPost("/summary", MealAllowanceQueryEndpoints.GetSummaryAsync);
        group.MapPost("/search", MealAllowanceQueryEndpoints.SearchAsync);
        group.MapPost("/search-page", MealAllowanceQueryEndpoints.SearchPageAsync);
        group.MapGet("/export-period/{year:int}/{month:int}", MealAllowanceQueryEndpoints.ExportAsync);
        group.MapPost("/refresh", MealAllowanceCommandEndpoints.RefreshAsync);
        group.MapPost("/manual-values", MealAllowanceCommandEndpoints.UpdateManualValuesAsync);
        group.MapPost("/lock-state/batch", MealAllowanceCommandEndpoints.SetLockStateBatchAsync);

        return payrollGroup;
    }
}
