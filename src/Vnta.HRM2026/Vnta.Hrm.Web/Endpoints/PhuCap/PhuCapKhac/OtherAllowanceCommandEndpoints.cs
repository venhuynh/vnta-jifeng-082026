using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Exceptions;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Command endpoint boundary for the other-allowance feature.</summary>
internal static class OtherAllowanceCommandEndpoints
{
    internal static Task<IResult> CreateAsync(
        [FromBody] CreateOtherAllowanceRequest? request,
        [FromServices] IOtherAllowanceCreateService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            request,
            (value, token) => service.CreateAsync(value with { RequestedBy = PayrollEndpoints.ResolveAuditActor(context.User) }, token),
            context,
            auditScope,
            correlationAccessor,
            AuditActions.OtherAllowance.Created,
            cancellationToken);

    internal static Task<IResult> UpdateAsync(
        [FromBody] UpdateOtherAllowanceRequest? request,
        [FromServices] IOtherAllowanceUpdateService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            request,
            (value, token) => service.UpdateAsync(value with { RequestedBy = PayrollEndpoints.ResolveAuditActor(context.User) }, token),
            context,
            auditScope,
            correlationAccessor,
            AuditActions.OtherAllowance.Updated,
            cancellationToken);

    internal static async Task<IResult> SyncFromPreviousMonthAsync(
        [FromBody] SyncOtherAllowanceFromPreviousMonthRequest? request,
        [FromServices] IOtherAllowancePreviousMonthSyncService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if(request is null) return Results.BadRequest(new { message = "Thiếu kỳ lương cần lấy dữ liệu phụ cấp khác." });
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                context,
                auditScope,
                correlationAccessor,
                AuditActions.OtherAllowance.SyncedFromPreviousMonth,
                token => service.SyncFromPreviousMonthAsync(
                    request with { RequestedBy = PayrollEndpoints.ResolveAuditActor(context.User) },
                    token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    internal static async Task<IResult> SetLockStateAsync(
        [FromBody] SetOtherAllowanceLockStateRequest? request,
        [FromServices] IOtherAllowanceLockService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if(request is null) return Results.BadRequest(new { message = "Thiếu dữ liệu khóa phụ cấp khác." });
        try
        {
            await PayrollEndpointExecution.ExecuteAsync(
                context,
                auditScope,
                correlationAccessor,
                AuditActions.OtherAllowance.LockStateChanged,
                token => service.SetLockStateAsync(
                    request with { RequestedBy = PayrollEndpoints.ResolveAuditActor(context.User) },
                    token),
                cancellationToken);
            return Results.NoContent();
        }
        catch(OtherAllowanceConflictException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch(KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
        catch(DbUpdateConcurrencyException) { return Results.Conflict(new { message = "Dá»¯ liá»‡u Ä‘Ã£ thay Ä‘á»•i bá»Ÿi thao tÃ¡c khÃ¡c. Vui lÃ²ng táº£i láº¡i." }); }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    internal static async Task<IResult> SetBatchLockStateAsync(
        [FromBody] SetOtherAllowanceBatchLockStateRequest? request,
        [FromServices] IOtherAllowanceLockService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if(request is null) return Results.BadRequest(new { message = "Thiếu dữ liệu khóa phụ cấp khác." });
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                context, auditScope, correlationAccessor, AuditActions.OtherAllowance.LockStateChanged,
                token => service.SetLockStateBatchAsync(request with { RequestedBy = PayrollEndpoints.ResolveAuditActor(context.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    internal static async Task<IResult> DeleteAsync(
        Guid id,
        [FromQuery] DateTime? originalUpdatedAtUtc,
        [FromServices] IOtherAllowanceDeleteService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await PayrollEndpointExecution.ExecuteAsync(
                context,
                auditScope,
                correlationAccessor,
                AuditActions.OtherAllowance.Deleted,
                token => service.DeleteAsync(
                    new DeleteOtherAllowanceRequest(id, originalUpdatedAtUtc, PayrollEndpoints.ResolveAuditActor(context.User)),
                    token),
                cancellationToken,
                metadata: CreateSelfApprovalMetadata(context));
            return Results.NoContent();
        }
        catch(OtherAllowanceConflictException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch(KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
        catch(DbUpdateConcurrencyException) { return Results.Conflict(new { message = "Dá»¯ liá»‡u Ä‘Ã£ thay Ä‘á»•i bá»Ÿi thao tÃ¡c khÃ¡c. Vui lÃ²ng táº£i láº¡i." }); }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static IReadOnlyDictionary<string, string> CreateSelfApprovalMetadata(HttpContext context) =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["approval.mode"] = "self",
            ["approval.approved_by"] = PayrollEndpoints.ResolveAuditActor(context.User)
        };

    private static async Task<IResult> ExecuteAsync<TRequest>(
        TRequest? request,
        Func<TRequest, CancellationToken, Task<OtherAllowanceCommandResult>> execute,
        HttpContext context,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string auditAction,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        if(request is null) return Results.BadRequest(new { message = "Thiếu dữ liệu phụ cấp khác." });
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                context,
                auditScope,
                correlationAccessor,
                auditAction,
                token => execute(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(OtherAllowanceConflictException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch(KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
        catch(DbUpdateConcurrencyException) { return Results.Conflict(new { message = "Dá»¯ liá»‡u Ä‘Ã£ thay Ä‘á»•i bá»Ÿi thao tÃ¡c khÃ¡c. Vui lÃ²ng táº£i láº¡i." }); }
        catch(InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
