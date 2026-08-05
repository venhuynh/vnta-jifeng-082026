using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints;

internal static class LuongCanBanEndpoints
{
    internal static RouteGroupBuilder MapLuongCanBanEndpoints(this RouteGroupBuilder payrollGroup)
    {
        payrollGroup.MapGet("/basic-salaries", GetBasicSalariesAsync);
        payrollGroup.MapGet("/basic-salaries/{id:guid}", GetBasicSalaryByIdAsync);
        payrollGroup.MapPost("/basic-salaries/search", SearchBasicSalariesAsync);
        payrollGroup.MapPost("/basic-salaries/validate", ValidateBasicSalaryAsync);
        payrollGroup.MapPost("/basic-salaries/sync-previous-month", SyncBasicSalariesFromPreviousMonthAsync);
        payrollGroup.MapPost("/basic-salaries", SaveBasicSalaryAsync);
        payrollGroup.MapPost("/basic-salaries/delete", DeleteBasicSalariesAsync);

        return payrollGroup;
    }

    private static async Task<IResult> GetBasicSalariesAsync(
        [FromServices] IBasicSalaryService basicSalaryService,
        CancellationToken cancellationToken)
    {
        var result = await basicSalaryService.GetAsync(cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBasicSalaryByIdAsync(
        [FromRoute] Guid id,
        [FromServices] IBasicSalaryService basicSalaryService,
        CancellationToken cancellationToken)
    {
        var result = await basicSalaryService.GetByIdAsync(id, cancellationToken);
        return result is null
            ? Results.NotFound(new { message = "Không tìm thấy bản ghi lương căn bản." })
            : Results.Ok(result);
    }

    private static async Task<IResult> SearchBasicSalariesAsync(
        [FromBody] BasicSalaryFilter? filter,
        [FromServices] IBasicSalaryService basicSalaryService,
        CancellationToken cancellationToken)
    {
        var result = await basicSalaryService.SearchAsync(
            filter ?? new BasicSalaryFilter(null),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> ValidateBasicSalaryAsync(
        [FromBody] UpsertBasicSalaryRecordRequest? request,
        [FromServices] IBasicSalaryService basicSalaryService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload lương căn bản." });
        }

        try
        {
            var validationMessage = await basicSalaryService.ValidateAsync(request, cancellationToken);
            return Results.Ok(validationMessage);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SyncBasicSalariesFromPreviousMonthAsync(
        [FromBody] SyncBasicSalaryFromPreviousMonthRequest? request,
        [FromServices] IBasicSalaryService basicSalaryService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload lấy dữ liệu lương căn bản từ tháng trước." });
        }

        try
        {
            var result = await basicSalaryService.SyncFromPreviousMonthAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SaveBasicSalaryAsync(
        [FromQuery] bool isNew,
        [FromBody] UpsertBasicSalaryRecordRequest? request,
        [FromServices] IBasicSalaryService basicSalaryService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload lương căn bản." });
        }

        try
        {
            var result = await basicSalaryService.SaveAsync(request, isNew, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteBasicSalariesAsync(
        [FromBody] IReadOnlyCollection<Guid>? ids,
        [FromServices] IBasicSalaryService basicSalaryService,
        CancellationToken cancellationToken)
    {
        if (ids is null)
        {
            return Results.BadRequest(new { message = "Thiếu danh sách lương căn bản cần xóa." });
        }

        try
        {
            await basicSalaryService.DeleteAsync(ids, cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
