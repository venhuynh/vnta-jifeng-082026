using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Endpoints;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruTongKet;

/// <summary>Command boundary for deduction summaries; the actor comes only from the authenticated principal.</summary>
internal static class KhauTruTongKetCommandEndpoints
{
    internal static RouteGroupBuilder MapKhauTruTongKetCommandEndpoints(this RouteGroupBuilder payrollGroup)
    {
        payrollGroup.MapPost("/deduction-summary/sync-previous-month", SyncAsync);
        payrollGroup.MapPost("/deduction-summary/refresh", RefreshAsync);
        payrollGroup.MapPost("/deduction-summary/recalculate", RecalculateAsync);
        payrollGroup.MapPost("/deduction-summary/manual-other-deduction", UpdateManualOtherDeductionAsync);
        payrollGroup.MapPost("/deduction-summary/lock-state", SetLockStateAsync);
        payrollGroup.MapPost("/deduction-summary/lock-state/batch", SetLockStateBatchAsync);
        return payrollGroup;
    }

    private static Task<IResult> SyncAsync([FromBody] SyncPayrollDeductionSummaryFromPreviousMonthRequest? request, [FromServices] IPayrollDeductionSummarySyncService service, [FromServices] IPayrollDeductionSummaryRequestValidator validator, HttpContext context, [FromServices] IAuditScope audit, [FromServices] IAuditCorrelationAccessor correlation, CancellationToken token)
    {
        if (request is null) return Task.FromResult<IResult>(Results.BadRequest(new { message = "Thieu payload tong ket khau tru." }));
        var validation = validator.Validate(request);
        if (!validation.IsValid) return Task.FromResult<IResult>(Results.BadRequest(new { message = validation.ErrorMessage }));
        return ExecuteAsync(context, audit, correlation, AuditActions.DeductionSummary.SyncFromPreviousMonth, t => service.SyncFromPreviousMonthAsync(request with { Actor = PayrollEndpoints.ResolveAuditActor(context.User) }, t), token);
    }

    private static Task<IResult> RefreshAsync([FromBody] RefreshPayrollDeductionSummaryRequest? request, [FromServices] IPayrollDeductionSummaryRefreshService service, [FromServices] IPayrollDeductionSummaryRequestValidator validator, HttpContext context, [FromServices] IAuditScope audit, [FromServices] IAuditCorrelationAccessor correlation, CancellationToken token)
    {
        if (request is null) return Task.FromResult<IResult>(Results.BadRequest(new { message = "Thiáº¿u payload lÃ m má»›i dÃ²ng tá»•ng káº¿t kháº¥u trá»«." }));
        var validation = validator.Validate(request);
        if (!validation.IsValid) return Task.FromResult<IResult>(Results.BadRequest(new { message = validation.ErrorMessage }));
        return ExecuteAsync(context, audit, correlation, AuditActions.DeductionSummary.Refreshed, t => service.RefreshAsync(request with { Actor = PayrollEndpoints.ResolveAuditActor(context.User) }, t), token);
    }

    private static Task<IResult> RecalculateAsync([FromBody] RecalculatePayrollDeductionSummaryPeriodRequest? request, [FromServices] IPayrollDeductionSummaryRefreshService service, [FromServices] IPayrollDeductionSummaryRequestValidator validator, HttpContext context, [FromServices] IAuditScope audit, [FromServices] IAuditCorrelationAccessor correlation, CancellationToken token)
    {
        if (request is null) return Task.FromResult<IResult>(Results.BadRequest(new { message = "Thiáº¿u ká»³ lÆ°Æ¡ng cáº§n tÃ­nh láº¡i tá»•ng káº¿t kháº¥u trá»«." }));
        var validation = validator.Validate(request);
        if (!validation.IsValid) return Task.FromResult<IResult>(Results.BadRequest(new { message = validation.ErrorMessage }));
        return ExecuteAsync(context, audit, correlation, AuditActions.DeductionSummary.PeriodRecalculated, t => service.RecalculatePeriodAsync(request with { Actor = PayrollEndpoints.ResolveAuditActor(context.User) }, t), token);
    }

    private static Task<IResult> UpdateManualOtherDeductionAsync([FromBody] UpdatePayrollDeductionSummaryManualOtherDeductionRequest? request, [FromServices] IPayrollDeductionSummaryManualAdjustmentService service, [FromServices] IPayrollDeductionSummaryRequestValidator validator, HttpContext context, [FromServices] IAuditScope audit, [FromServices] IAuditCorrelationAccessor correlation, CancellationToken token)
    {
        if (request is null) return Task.FromResult<IResult>(Results.BadRequest(new { message = "Thiáº¿u payload Ä‘iá»u chá»‰nh khoáº£n kháº¥u trá»« khÃ¡c." }));
        var validation = validator.Validate(request);
        if (!validation.IsValid) return Task.FromResult<IResult>(Results.BadRequest(new { message = validation.ErrorMessage }));
        return ExecuteAsync(context, audit, correlation, AuditActions.DeductionSummary.ManualOtherDeductionUpdated, t => service.UpdateManualOtherDeductionAsync(request with { Actor = PayrollEndpoints.ResolveAuditActor(context.User) }, t), token);
    }

    private static Task<IResult> SetLockStateAsync([FromBody] SetPayrollDeductionSummaryLockStateRequest? request, [FromServices] IPayrollDeductionSummaryLockService service, [FromServices] IPayrollDeductionSummaryRequestValidator validator, HttpContext context, [FromServices] IAuditScope audit, [FromServices] IAuditCorrelationAccessor correlation, CancellationToken token)
    {
        if (request is null) return Task.FromResult<IResult>(Results.BadRequest(new { message = "Thieu payload cap nhat trang thai khoa tong ket khau tru." }));
        var validation = validator.Validate(request);
        if (!validation.IsValid) return Task.FromResult<IResult>(Results.BadRequest(new { message = validation.ErrorMessage }));
        return ExecuteAsync(context, audit, correlation, AuditActions.DeductionSummary.LockStateChanged, t => service.SetLockStateAsync(request with { Actor = PayrollEndpoints.ResolveAuditActor(context.User) }, t), token);
    }

    private static Task<IResult> SetLockStateBatchAsync([FromBody] SetPayrollDeductionSummaryBatchLockStateRequest? request, [FromServices] IPayrollDeductionSummaryLockService service, [FromServices] IPayrollDeductionSummaryRequestValidator validator, HttpContext context, [FromServices] IAuditScope audit, [FromServices] IAuditCorrelationAccessor correlation, CancellationToken token)
    {
        if (request is null) return Task.FromResult<IResult>(Results.BadRequest(new { message = "Thieu payload cap nhat hang loat trang thai khoa tong ket khau tru." }));
        var validation = validator.Validate(request);
        if (!validation.IsValid) return Task.FromResult<IResult>(Results.BadRequest(new { message = validation.ErrorMessage }));
        return ExecuteAsync(context, audit, correlation, AuditActions.DeductionSummary.BatchLockStateChanged, t => service.SetLockStateBatchAsync(request with { Actor = PayrollEndpoints.ResolveAuditActor(context.User) }, t), token);
    }

    private static async Task<IResult> ExecuteAsync<T>(HttpContext context, IAuditScope audit, IAuditCorrelationAccessor correlation, string action, Func<CancellationToken, Task<T>> command, CancellationToken token)
    {
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(context, audit, correlation, action, command, token, AuditCaptureMode.OperationOnly);
            return Results.Ok(result);
        }
        catch (PayrollDeductionSummaryConcurrencyException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
