using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Read-only HTTP boundary for meal allowance.</summary>
internal static class MealAllowanceQueryEndpoints
{
    internal static async Task<IResult> SearchAsync(
        [FromBody] MealAllowanceFilter? filter,
        [FromServices] IMealAllowanceReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.SearchAsync(filter ?? new(null, null, null), cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> SearchPageAsync(
        [FromBody] MealAllowanceFilter? filter,
        [FromServices] IMealAllowanceReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.SearchPageAsync(filter ?? new(null, null, null), cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> GetSummaryAsync(
        [FromBody] MealAllowanceFilter? filter,
        [FromServices] IMealAllowanceReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.GetSummaryAsync(filter ?? new(null, null, null), cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> ExportAsync(
        int year,
        int month,
        [FromServices] IMealAllowanceExportService service,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await service.ExportPeriodAsync(month, year, cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
