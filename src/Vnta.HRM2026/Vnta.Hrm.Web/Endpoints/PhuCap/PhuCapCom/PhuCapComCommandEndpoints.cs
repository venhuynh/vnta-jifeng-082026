using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Command HTTP boundary for meal allowance.</summary>
internal static class MealAllowanceCommandEndpoints
{
    internal static async Task<IResult> RefreshAsync(
        [FromBody] RefreshMealAllowanceRequest? request,
        [FromServices] IMealAllowanceRefreshService service,
        [FromServices] IMealAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest();
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.MealAllowance.Refreshed,
                token => service.RefreshAsync(
                    request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(MealAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> UpdateManualValuesAsync(
        [FromBody] UpdateMealAllowanceManualValuesRequest? request,
        [FromServices] IMealAllowanceManualAdjustmentService service,
        [FromServices] IMealAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiếu dữ liệu điều chỉnh phụ cấp cơm." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.MealAllowance.ManualValuesUpdated,
                token => service.UpdateManualValuesAsync(
                    request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(MealAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> SetLockStateBatchAsync(
        [FromBody] SetMealAllowanceLockStateBatchRequest? request,
        [FromServices] IMealAllowanceLockService service,
        [FromServices] IMealAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiếu dữ liệu khóa phụ cấp cơm." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.MealAllowance.BatchLockStateChanged,
                token => service.SetLockStateBatchAsync(
                    request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(MealAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

}
