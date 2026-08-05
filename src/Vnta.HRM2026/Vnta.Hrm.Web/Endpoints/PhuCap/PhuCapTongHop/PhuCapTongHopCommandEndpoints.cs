using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Audited command HTTP boundary for the allowance-summary feature.</summary>
internal static class PhuCapTongHopCommandEndpoints
{
    internal static async Task<IResult> SyncFromPreviousMonthAsync(
        [FromBody] SyncPayrollAllowanceSummaryFromPreviousMonthRequest? request,
        [FromServices] IPayrollAllowanceSummaryPreviousMonthSyncService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload phụ cấp tổng hợp." });
        }

        return await ExecuteCommandAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.AllowanceSummary.SyncFromPreviousMonth,
            token => service.SyncFromPreviousMonthAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> RefreshAsync(
        [FromBody] RefreshPayrollAllowanceSummaryRequest? request,
        [FromServices] IPayrollAllowanceSummaryRefreshService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload làm mới phụ cấp." });
        }

        return await ExecuteCommandAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.AllowanceSummary.Refreshed,
            token => service.RefreshAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> DeleteAsync(
        [FromBody] DeletePayrollAllowanceSummariesRequest? request,
        [FromServices] IPayrollAllowanceSummaryDeletionService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu danh sách dòng tổng hợp phụ cấp cần xóa." });
        }

        return await ExecuteDeleteCommandAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.AllowanceSummary.Deleted,
            token => service.DeleteAsync(request, token),
            cancellationToken);
    }

    internal static async Task<IResult> UpdateManualValuesAsync(
        [FromBody] UpdatePayrollAllowanceSummaryManualValuesRequest? request,
        [FromServices] IPayrollAllowanceSummaryManualAdjustmentService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload cập nhật giá trị nhập tay." });
        }

        return await ExecuteCommandAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.AllowanceSummary.ManualValuesUpdated,
            token => service.UpdateManualValuesAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> SetLockStateAsync(
        [FromBody] SetPayrollAllowanceSummaryLockStateRequest? request,
        [FromServices] IPayrollAllowanceSummaryLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload cập nhật trạng thái khóa." });
        }

        return await ExecuteCommandAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.AllowanceSummary.LockStateChanged,
            token => service.SetLockStateAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken);
    }

    internal static async Task<IResult> SetLockStateBatchAsync(
        [FromBody] SetPayrollAllowanceSummaryBatchLockStateRequest? request,
        [FromServices] IPayrollAllowanceSummaryLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa hàng loạt phụ cấp tổng hợp." });
        }

        return await ExecuteCommandAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.AllowanceSummary.BatchLockStateChanged,
            token => service.SetLockStateBatchAsync(
                request with { Actor = PayrollEndpoints.ResolveAuditActor(httpContext.User) }, token),
            cancellationToken,
            AuditCaptureMode.OperationOnly);
    }

    private static async Task<IResult> ExecuteCommandAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string actionIntent,
        Func<CancellationToken, Task<T>> command,
        CancellationToken cancellationToken,
        AuditCaptureMode captureMode = AuditCaptureMode.EntityChanges)
    {
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, actionIntent, command, cancellationToken, captureMode);
            return Results.Ok(result);
        }
        catch (DbUpdateConcurrencyException ex)
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

    private static async Task<IResult> ExecuteDeleteCommandAsync(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string actionIntent,
        Func<CancellationToken, Task> command,
        CancellationToken cancellationToken)
    {
        try
        {
            await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, actionIntent, command, cancellationToken);
            return Results.NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
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
