using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Command endpoint boundary for hazard allowance.</summary>
internal static class HazardAllowanceCommandEndpoints
{
    internal static async Task<IResult> QueueExportAsync(
        [FromBody] HazardAllowanceFilter? filter,
        [FromServices] IHazardAllowanceExportJobService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if(filter is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện export phụ cấp độc hại." });
        }
        var validation = requestValidator.Validate(new CreateHazardAllowanceExportJobRequest(
            filter,
            HazardAllowanceEndpointExecution.ResolveActor(context.User)));
        if(!validation.IsValid)
        {
            return Results.BadRequest(new { message = validation.ErrorMessage });
        }

        try
        {
            var job = await service.QueueAsync(
                new CreateHazardAllowanceExportJobRequest(
                    filter,
                    HazardAllowanceEndpointExecution.ResolveActor(context.User)),
                cancellationToken);
            return Results.Accepted($"/api/payroll/hazard-allowance/export-jobs/{job.Id:D}", job);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> RefreshAsync(
        [FromBody] RefreshHazardAllowanceRequest? request,
        [FromServices] IHazardAllowanceRefreshService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        HttpContext context,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiếu payload làm mới phụ cấp độc hại." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                context, auditScope, correlationAccessor, AuditActions.HazardAllowance.Refreshed,
                token => service.RefreshAsync(
                    request with { RequestedBy = HazardAllowanceEndpointExecution.ResolveActor(context.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(HazardAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> UpdateManualValuesAsync(
        [FromBody] UpdateHazardAllowanceManualValuesRequest? request,
        [FromServices] IHazardAllowanceManualAdjustmentService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        HttpContext context,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiếu payload điều chỉnh phụ cấp độc hại." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                context, auditScope, correlationAccessor, AuditActions.HazardAllowance.ManualValuesUpdated,
                token => service.UpdateManualValuesAsync(
                    request with { RequestedBy = HazardAllowanceEndpointExecution.ResolveActor(context.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(HazardAllowanceConflictException ex)
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

    internal static async Task<IResult> SetLockStateAsync(
        [FromBody] SetHazardAllowanceLockStateRequest? request,
        [FromServices] IHazardAllowanceLockService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        HttpContext context,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiếu payload khóa phụ cấp độc hại." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            await PayrollEndpointExecution.ExecuteAsync(
                context, auditScope, correlationAccessor, AuditActions.HazardAllowance.LockStateChanged,
                token => service.SetLockStateAsync(
                    request with { RequestedBy = HazardAllowanceEndpointExecution.ResolveActor(context.User) }, token),
                cancellationToken);
            return Results.NoContent();
        }
        catch(HazardAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> SetEntitlementBatchAsync(
        [FromBody] SetHazardAllowanceEntitlementBatchRequest? request,
        [FromServices] IHazardAllowanceEntitlementService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        HttpContext context,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiếu dữ liệu cập nhật trạng thái hưởng phụ cấp độc hại." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                context, auditScope, correlationAccessor, AuditActions.HazardAllowance.EntitlementBatchUpdated,
                token => service.SetEntitlementBatchAsync(
                    request with { RequestedBy = HazardAllowanceEndpointExecution.ResolveActor(context.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(HazardAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> SetLockStateBatchAsync(
        [FromBody] SetHazardAllowanceBatchLockStateRequest? request,
        [FromServices] IHazardAllowanceLockService service,
        [FromServices] IHazardAllowanceRequestValidator requestValidator,
        HttpContext context,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiếu payload khóa phụ cấp độc hại." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                context, auditScope, correlationAccessor, AuditActions.HazardAllowance.BatchLockStateChanged,
                token => service.SetLockStateBatchAsync(
                    request with { RequestedBy = HazardAllowanceEndpointExecution.ResolveActor(context.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(HazardAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
