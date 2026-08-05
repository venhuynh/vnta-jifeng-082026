using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruPhiCongDoan;

internal static class KhauTruPhiCongDoanQueryEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/union-fee-deductions/search", SearchAsync);

    private static async Task<IResult> SearchAsync(
        [FromBody] PayrollUnionFeeDeductionFilter? filter,
        [FromServices] IPayrollUnionFeeDeductionReadService readService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await readService.SearchAsync(
                filter ?? new PayrollUnionFeeDeductionFilter(null, null, null),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
