using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Đăng ký HTTP contract cho dashboard khấu trừ dưới payroll group đã được phân quyền.</summary>
public static partial class PayrollEndpoints
{
    private static RouteGroupBuilder MapKhauTruDashboardEndpoints(this RouteGroupBuilder payrollGroup)
    {
        payrollGroup.MapPost("/deduction-summary/dashboard", GetPayrollDeductionDashboardAsync);
        return payrollGroup;
    }

    private static async Task<IResult> GetPayrollDeductionDashboardAsync(
        [FromBody] PayrollDeductionDashboardFilter? filter,
        [FromServices] IPayrollDeductionDashboardService dashboardService,
        CancellationToken cancellationToken)
    {
        if(filter is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện tải dashboard khấu trừ." });
        }

        try
        {
            return Results.Ok(await dashboardService.GetDashboardAsync(filter, cancellationToken));
        }
        catch(InvalidOperationException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }
}
