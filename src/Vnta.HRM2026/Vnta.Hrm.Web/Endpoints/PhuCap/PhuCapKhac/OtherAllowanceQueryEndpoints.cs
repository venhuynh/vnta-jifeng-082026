using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Queries;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Read endpoint boundary for the other-allowance feature.</summary>
internal static class OtherAllowanceQueryEndpoints
{
    internal static async Task<IResult> SearchAsync(
        [FromBody] OtherAllowanceFilter? filter,
        [FromServices] IOtherAllowanceReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.Today;
            return Results.Ok(await service.SearchPageAsync(
                filter ?? new OtherAllowanceFilter(today.Month, today.Year),
                cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
