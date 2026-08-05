using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiemKhac;

/// <summary>Read-only HTTP boundary for other responsibility allowance.</summary>
internal static class OtherResponsibilityAllowanceQueryEndpoints
{
    internal static RouteGroupBuilder MapOtherResponsibilityAllowanceQueryEndpoints(this RouteGroupBuilder featureGroup)
    {
        featureGroup.MapPost("/search", SearchAsync);
        return featureGroup;
    }

    internal static async Task<IResult> SearchAsync(
        [FromBody] OtherResponsibilityAllowanceFilter? filter,
        [FromServices] IOtherResponsibilityAllowanceReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var appliedFilter = filter ?? CreateDefaultFilter();
            return Results.Ok(await service.SearchAsync(
                appliedFilter,
                cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static OtherResponsibilityAllowanceFilter CreateDefaultFilter()
    {
        var today = DateTime.Today;
        var fallbackYear = Math.Clamp(today.Year, 2026, 2100);
        var fallbackMonth = fallbackYear == 2026 ? Math.Max(today.Month, 6) : today.Month;
        return new OtherResponsibilityAllowanceFilter(fallbackMonth, fallbackYear, null);
    }
}
