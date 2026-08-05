using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Read endpoint boundary for hazard allowance.</summary>
internal static class HazardAllowanceQueryEndpoints
{
    internal static async Task<IResult> SearchAsync(
        [FromBody] HazardAllowanceFilter? filter,
        [FromServices] IHazardAllowanceReadService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        CancellationToken cancellationToken)
    {
        var resolvedFilter = HazardAllowanceEndpointExecution.ResolveFilter(filter);
        var validation = requestValidator.Validate(resolvedFilter);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        var result = await service.SearchAsync(
            resolvedFilter,
            cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> SearchPageAsync(
        [FromBody] HazardAllowanceFilter? filter,
        [FromServices] IHazardAllowanceReadService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        CancellationToken cancellationToken)
    {
        var resolvedFilter = HazardAllowanceEndpointExecution.ResolveFilter(filter);
        var validation = requestValidator.Validate(resolvedFilter);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        var result = await service.SearchPageAsync(
            resolvedFilter,
            cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> SummaryAsync(
        [FromBody] HazardAllowanceFilter? filter,
        [FromServices] IHazardAllowanceReadService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        CancellationToken cancellationToken)
    {
        var resolvedFilter = HazardAllowanceEndpointExecution.ResolveFilter(filter);
        var validation = requestValidator.Validate(resolvedFilter);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        var result = await service.GetSummaryAsync(
            resolvedFilter,
            cancellationToken);
        return Results.Ok(result);
    }

    internal static async Task<IResult> ExportAsync(
        [FromBody] HazardAllowanceFilter? filter,
        [FromServices] IHazardAllowanceExportService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        CancellationToken cancellationToken)
    {
        if(filter is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện xuất phụ cấp độc hại." });
        }

        var validation = requestValidator.Validate(filter);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        return Results.Ok(await service.ExportAsync(filter, cancellationToken));
    }
}
