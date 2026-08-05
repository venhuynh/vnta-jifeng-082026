using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints;

public static partial class PayrollEndpoints
{
    private static async Task<IResult> SearchSeniorityAllowancesAsync(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] string? departmentName,
        [FromQuery] string? searchText, [FromQuery] bool? isLocked, [FromQuery] int? take,
        [FromQuery] string? seniorityRangeKey,
        [FromServices] IPayrollEmployeeSeniorityAllowanceReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SearchAsync(new PayrollEmployeeSeniorityAllowanceFilter(
                month, year, departmentName, searchText, isLocked, take ?? 2000, 0, seniorityRangeKey), cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> SearchSeniorityAllowancePageAsync(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] string? departmentName,
        [FromQuery] string? searchText, [FromQuery] bool? isLocked, [FromQuery] int? take,
        [FromQuery] int? skip, [FromQuery] string? seniorityRangeKey,
        [FromServices] IPayrollEmployeeSeniorityAllowanceReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SearchPageAsync(new PayrollEmployeeSeniorityAllowanceFilter(
                month, year, departmentName, searchText, isLocked, take ?? 2000, skip ?? 0, seniorityRangeKey), cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> GetSeniorityAllowanceRangeSummariesAsync(
        [FromQuery] int year, [FromQuery] int month, [FromQuery] string? departmentName,
        [FromQuery] string? searchText, [FromQuery] bool? isLocked,
        [FromServices] IPayrollEmployeeSeniorityAllowanceRangeSummaryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.GetRangeSummariesAsync(
                new PayrollEmployeeSeniorityAllowanceFilter(month, year, departmentName, searchText, isLocked), cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
