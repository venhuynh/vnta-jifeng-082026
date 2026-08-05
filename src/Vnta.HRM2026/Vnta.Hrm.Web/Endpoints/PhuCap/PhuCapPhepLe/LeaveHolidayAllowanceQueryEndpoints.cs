using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints;

internal static class LeaveHolidayAllowanceQueryEndpoints
{
    internal static async Task<IResult> SearchAsync(
        [FromBody] LeaveHolidayAllowanceFilter? filter,
        [FromServices] ILeaveHolidayAllowanceReadService service,
        [FromServices] ILeaveHolidayAllowanceRequestValidator requestValidator,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var resolvedFilter = filter ?? new LeaveHolidayAllowanceFilter(today.Month, today.Year, null);
        var validation = requestValidator.Validate(resolvedFilter);
        if (!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await service.SearchAsync(resolvedFilter, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
