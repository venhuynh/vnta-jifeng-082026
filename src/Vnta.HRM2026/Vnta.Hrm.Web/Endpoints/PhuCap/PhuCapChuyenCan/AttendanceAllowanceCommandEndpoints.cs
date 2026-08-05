using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Exceptions;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapChuyenCan;

/// <summary>Mutating HTTP boundary for attendance allowance.</summary>
internal static class AttendanceAllowanceCommandEndpoints
{
    internal static async Task<IResult> RefreshAsync(
        [FromBody] RefreshAttendanceAllowanceRequest? request,
        [FromServices] IAttendanceAllowanceRefreshService commandService,
        [FromServices] IAttendanceAllowanceRefreshRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiáº¿u payload phá»¥ cáº¥p." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, AuditActions.AttendanceAllowance.Refresh,
                token => commandService.RefreshAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch(AttendanceAllowanceCommandException ex)
        {
            return AttendanceAllowanceEndpointExecution.MapCommandException(ex);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> UpdateActualWorkdayAsync(
        [FromBody] UpdateAttendanceAllowanceActualWorkdayRequest? request,
        [FromServices] IAttendanceAllowanceManualAdjustmentService commandService,
        [FromServices] IAttendanceAllowanceManualAdjustmentRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiáº¿u payload Ä‘iá»u chá»‰nh sá»‘ ngÃ y cÃ´ng thá»±c táº¿ phá»¥ cáº¥p chuyÃªn cáº§n." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        return await ExecuteAsync(
            httpContext, auditScope, correlationAccessor, AuditActions.AttendanceAllowance.Save,
            token => commandService.UpdateActualWorkdayAsync(request, token), cancellationToken);
    }

    internal static async Task<IResult> UpdateStandardWorkdayAsync(
        [FromBody] UpdateAttendanceAllowanceStandardWorkdayRequest? request,
        [FromServices] IAttendanceAllowanceManualAdjustmentService commandService,
        [FromServices] IAttendanceAllowanceManualAdjustmentRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiáº¿u payload Ä‘iá»u chá»‰nh sá»‘ ngÃ y cÃ´ng chuáº©n phá»¥ cáº¥p chuyÃªn cáº§n." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        return await ExecuteAsync(
            httpContext, auditScope, correlationAccessor, AuditActions.AttendanceAllowance.Save,
            token => commandService.UpdateStandardWorkdayAsync(request, token), cancellationToken);
    }

    internal static async Task<IResult> UpdateWorkdaysAsync(
        [FromBody] UpdateAttendanceAllowanceWorkdaysRequest? request,
        [FromServices] IAttendanceAllowanceWorkdayAdjustmentService commandService,
        [FromServices] AttendanceAllowanceWorkdayAdjustmentPolicy workdayAdjustmentPolicy,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiếu payload điều chỉnh ngày công phụ cấp chuyên cần." });

        var validation = workdayAdjustmentPolicy.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        return await ExecuteAsync(
            httpContext, auditScope, correlationAccessor, AuditActions.AttendanceAllowance.Save,
            token => commandService.UpdateWorkdaysAsync(request, token), cancellationToken);
    }

    internal static async Task<IResult> SetLockStateAsync(
        [FromBody] SetAttendanceAllowanceLockStateRequest? request,
        [FromServices] IAttendanceAllowanceLockService commandService,
        [FromServices] IAttendanceAllowanceLockStateRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiáº¿u payload khÃ³a hoáº·c má»Ÿ khÃ³a phá»¥ cáº¥p chuyÃªn cáº§n." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        return await ExecuteAsync(
            httpContext, auditScope, correlationAccessor, AuditActions.AttendanceAllowance.SetLockState,
            token => commandService.SetLockStateAsync(request, token), cancellationToken);
    }

    internal static async Task<IResult> SetLockStateBatchAsync(
        [FromBody] SetAttendanceAllowanceBatchLockStateRequest? request,
        [FromServices] IAttendanceAllowanceLockService commandService,
        [FromServices] IAttendanceAllowanceBatchLockRequestValidator requestValidator,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
            return Results.BadRequest(new { message = "Thiáº¿u payload khÃ³a phá»¥ cáº¥p chuyÃªn cáº§n." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        return await ExecuteAsync(
            httpContext, auditScope, correlationAccessor, AuditActions.AttendanceAllowance.SetLockStateBatch,
            token => commandService.SetLockStateBatchAsync(request, token), cancellationToken);
    }

    private static async Task<IResult> ExecuteAsync<T>(
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
        catch(AttendanceAllowanceCommandException ex)
        {
            return AttendanceAllowanceEndpointExecution.MapCommandException(ex);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

}
