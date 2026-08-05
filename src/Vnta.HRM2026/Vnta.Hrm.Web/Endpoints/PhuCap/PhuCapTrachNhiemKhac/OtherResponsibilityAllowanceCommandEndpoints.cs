using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiemKhac;

/// <summary>Mutating HTTP boundary for other responsibility allowance.</summary>
internal static class OtherResponsibilityAllowanceCommandEndpoints
{
    internal static RouteGroupBuilder MapOtherResponsibilityAllowanceCommandEndpoints(this RouteGroupBuilder featureGroup)
    {
        featureGroup.MapPost("/prepare-period", PreparePeriodAsync);
        featureGroup.MapPost("/recalculate", RecalculateAsync);
        featureGroup.MapPost("/lock-state/batch", SetBatchLockStateAsync);
        return featureGroup;
    }

    internal static async Task<IResult> PreparePeriodAsync(
        HttpContext httpContext,
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IOtherResponsibilityAllowancePeriodPreparationService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        try
        {
            await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.OtherResponsibilityAllowance.PeriodPrepared,
                token => service.PreparePeriodAsync(
                    year,
                    month,
                    PayrollEndpoints.ResolveAuditActor(httpContext.User),
                    token),
                cancellationToken);
            return Results.NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> RecalculateAsync(
        HttpContext httpContext,
        [FromBody] RecalculateOtherResponsibilityAllowanceRequest? request,
        [FromServices] IOtherResponsibilityAllowanceRecalculationService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu kỳ lương cần tính lại phụ cấp trách nhiệm khác." });
        }
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.OtherResponsibilityAllowance.Recalculated,
                token => service.RecalculateAsync(
                    request,
                    PayrollEndpoints.ResolveAuditActor(httpContext.User),
                    token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = "Dá»¯ liá»‡u Ä‘Ã£ thay Ä‘á»•i bá»Ÿi thao tÃ¡c khÃ¡c. Vui lÃ²ng táº£i láº¡i." });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> SetBatchLockStateAsync(
        HttpContext httpContext,
        [FromBody] SetOtherResponsibilityAllowanceBatchLockStateRequest? request,
        [FromServices] IOtherResponsibilityAllowanceLockService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiáº¿u payload khÃ³a hoáº·c má»Ÿ khÃ³a phá»¥ cáº¥p trÃ¡ch nhiá»‡m khÃ¡c." });
        }
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.OtherResponsibilityAllowance.BatchLockStateChanged,
                token => service.SetLockStateBatchAsync(
                    request,
                    PayrollEndpoints.ResolveAuditActor(httpContext.User),
                    token),
                cancellationToken,
                metadata: new Dictionary<string, string>
                {
                    ["scope"] = request.PayrollAllowanceSummaryRecordIds is null ? "whole-period" : "selected-rows",
                    ["payrollPeriod"] = $"{request.PayrollMonth:00}/{request.PayrollYear}",
                    ["isLocked"] = request.IsLocked.ToString()
                });
            return Results.Ok(result);
        }
        catch(OtherResponsibilityAllowanceConcurrencyException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { message = "Dá»¯ liá»‡u Ä‘Ã£ thay Ä‘á»•i bá»Ÿi thao tÃ¡c khÃ¡c. Vui lÃ²ng táº£i láº¡i." });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
