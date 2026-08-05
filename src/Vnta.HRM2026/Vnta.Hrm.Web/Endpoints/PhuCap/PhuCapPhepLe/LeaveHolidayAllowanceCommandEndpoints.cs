using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

#pragma warning disable CS0618 // These endpoints are the remaining legacy compatibility surface.

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>HTTP command boundary for the leave/holiday allowance feature.</summary>
internal static class LeaveHolidayAllowanceCommandEndpoints
{
    internal static async Task<IResult> PreparePeriodAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] ILeaveHolidayAllowancePeriodPreparationService service,
        [FromServices] ILeaveHolidayAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (!requestValidator.ValidatePeriod(month, year).IsValid)
            return Results.BadRequest(new { message = "Invalid leave/holiday allowance period." });
        try
        {
            await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor,
                AuditActions.LeaveHolidayAllowance.PreparePeriod,
                token => service.PreparePeriodAsync(year, month, token), cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> ClearManualValuesAsync(
        [FromBody] ClearLeaveHolidayAllowanceManualValuesRequest? request,
        [FromServices] ILeaveHolidayAllowanceClearManualValuesService service,
        [FromServices] ILeaveHolidayAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload xóa dữ liệu nhập tay phụ cấp Phép - Lễ." });
        if (!requestValidator.Validate(request).IsValid)
            return Results.BadRequest(new { message = "Invalid leave/holiday allowance clear payload." });
        return await ExecuteCommandAsync(
            httpContext, auditScope, correlationAccessor,
            AuditActions.LeaveHolidayAllowance.ClearManualValues,
            token => service.ClearManualValuesAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> SyncFromPreviousMonthAsync(
        [FromBody] SyncLeaveHolidayAllowanceFromPreviousMonthRequest? request,
        [FromServices] ILeaveHolidayAllowancePreviousMonthSyncService service,
        [FromServices] ILeaveHolidayAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload đồng bộ phụ cấp Phép - Lễ từ tháng trước." });
        if (!requestValidator.Validate(request).IsValid)
            return Results.BadRequest(new { message = "Invalid leave/holiday allowance period." });
        return await ExecuteCommandAsync(
            httpContext, auditScope, correlationAccessor,
            AuditActions.LeaveHolidayAllowance.SyncFromPreviousMonth,
            token => service.SyncFromPreviousMonthAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> RecalculateAsync(
        [FromBody] RecalculateLeaveHolidayAllowanceRequest? request,
        [FromServices] ILeaveHolidayAllowanceRecalculationService service,
        [FromServices] ILeaveHolidayAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload tính lại phụ cấp Phép - Lễ." });
        if (!requestValidator.Validate(request).IsValid)
            return Results.BadRequest(new { message = "Invalid leave/holiday allowance recalculate payload." });
        return await ExecuteCommandAsync(
            httpContext, auditScope, correlationAccessor,
            AuditActions.LeaveHolidayAllowance.Recalculate,
            token => service.RecalculateAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> UpdateManualValuesAsync(
        [FromBody] UpdateLeaveHolidayAllowanceManualValuesRequest? request,
        [FromServices] ILeaveHolidayAllowanceManualAdjustmentService service,
        [FromServices] ILeaveHolidayAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload cập nhật phụ cấp Phép - Lễ." });
        if (!requestValidator.Validate(request).IsValid)
        {
            return Results.BadRequest(new { message = "Invalid leave/holiday allowance manual-values payload." });
        }
        return await ExecuteCommandAsync(
            httpContext, auditScope, correlationAccessor,
            AuditActions.LeaveHolidayAllowance.ManualValuesUpdated,
            token => service.UpdateManualValuesAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> SetLockStateAsync(
        [FromBody] SetLeaveHolidayAllowanceLockStateRequest? request,
        [FromServices] ILeaveHolidayAllowanceLockService service,
        [FromServices] ILeaveHolidayAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa phụ cấp Phép - Lễ." });
        if (!requestValidator.Validate(request).IsValid)
            return Results.BadRequest(new { message = "Invalid leave/holiday allowance lock payload." });
        return await ExecuteCommandAsync(
            httpContext, auditScope, correlationAccessor,
            AuditActions.LeaveHolidayAllowance.LockStateChanged,
            token => service.SetLockStateAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> SetLockStateBatchAsync(
        [FromBody] SetLeaveHolidayAllowanceBatchLockStateRequest? request,
        [FromServices] ILeaveHolidayAllowanceLockService service,
        [FromServices] ILeaveHolidayAllowanceRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa hàng loạt phụ cấp Phép - Lễ." });
        if (!requestValidator.Validate(request).IsValid)
        {
            return Results.BadRequest(new { message = "Invalid leave/holiday allowance batch-lock payload." });
        }
        return await ExecuteCommandAsync(
            httpContext, auditScope, correlationAccessor,
            AuditActions.LeaveHolidayAllowance.BatchLockStateChanged,
            token => service.SetLockStateBatchAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    private static async Task<IResult> ExecuteCommandAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string action,
        Func<CancellationToken, Task<T>> command,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, action, command, cancellationToken);
            return Results.Ok(result);
        }
        catch (LeaveHolidayAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
