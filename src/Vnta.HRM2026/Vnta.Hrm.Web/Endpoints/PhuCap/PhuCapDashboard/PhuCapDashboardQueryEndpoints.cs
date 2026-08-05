using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints;

internal static class PhuCapDashboardQueryEndpoints
{
    internal static async Task<IResult> GetDashboardAsync(
        [FromBody] PayrollAllowanceDashboardFilter? filter,
        [FromServices] IPayrollAllowanceDashboardReadService service,
        CancellationToken cancellationToken)
    {
        if (filter is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện tải dashboard phụ cấp." });
        }

        try
        {
            return Results.Ok(await service.GetDashboardAsync(filter, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static Task<IResult> GetBreakdownAsync([FromBody] PayrollAllowanceDashboardFilter? filter, [FromServices] IPayrollAllowanceDashboardBreakdownQueryService service, CancellationToken cancellationToken) =>
        ExecuteAsync(filter, service.GetAllowanceBreakdownAsync, cancellationToken);
    internal static Task<IResult> GetTrendAsync([FromBody] PayrollAllowanceDashboardFilter? filter, [FromServices] IPayrollAllowanceDashboardTrendQueryService service, CancellationToken cancellationToken) =>
        ExecuteAsync(filter, service.GetTrendAsync, cancellationToken);
    internal static Task<IResult> GetMonthlyComparisonAsync([FromBody] PayrollAllowanceDashboardFilter? filter, [FromServices] IPayrollAllowanceDashboardMonthlyComparisonQueryService service, CancellationToken cancellationToken) =>
        ExecuteAsync(filter, service.GetAllowanceMonthlyComparisonAsync, cancellationToken);
    internal static Task<IResult> GetDepartmentMonthlyComparisonAsync([FromBody] PayrollAllowanceDashboardFilter? filter, [FromServices] IPayrollAllowanceDashboardDepartmentComparisonQueryService service, CancellationToken cancellationToken) =>
        ExecuteAsync(filter, service.GetDepartmentMonthlyComparisonAsync, cancellationToken);

    private static async Task<IResult> ExecuteAsync<T>(PayrollAllowanceDashboardFilter? filter, Func<PayrollAllowanceDashboardFilter, CancellationToken, Task<T>> query, CancellationToken cancellationToken)
    {
        if (filter is null)
            return Results.BadRequest(new { message = "Thiếu điều kiện tải dashboard phụ cấp." });
        try { return Results.Ok(await query(filter, cancellationToken)); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
