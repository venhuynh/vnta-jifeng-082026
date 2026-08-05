using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.TinhLuong.BangCongTongHop;

namespace Vnta.Hrm.Web.Endpoints;

public static partial class PayrollEndpoints
{
    private static RouteGroupBuilder MapBangCongTongHopEndpoints(this RouteGroupBuilder payrollGroup)
    {
        payrollGroup.MapPost("/monthly-work-inputs/refresh", RefreshPayrollMonthlyWorkInputsAsync);
        return payrollGroup;
    }

    private static async Task<IResult> RefreshPayrollMonthlyWorkInputsAsync(
        [FromBody] RefreshPayrollMonthlyWorkInputsRequest? request,
        [FromServices] IPayrollMonthlyWorkInputRefreshService service,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu tháng và năm kỳ lương cần tổng hợp." });
        }

        try
        {
            return Results.Ok(await service.RefreshAsync(request, cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
