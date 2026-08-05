using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruThueTNCN;

internal static class KhauTruThueTNCNCommandEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/personal-income-tax-deductions/refresh", RefreshAsync);
        endpoints.MapPost("/personal-income-tax-deductions/manual-value", UpdateManualValueAsync);
        endpoints.MapPost("/personal-income-tax-deductions/lock-state/batch", SetLockStateBatchAsync);
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshPayrollPersonalIncomeTaxDeductionRequest? request,
        [FromServices] IPayrollPersonalIncomeTaxDeductionRefreshService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(request, service.RefreshAsync, AuditActions.PersonalIncomeTaxDeduction.Refreshed,
            auditScope, correlationAccessor, context, cancellationToken, "Thiếu payload làm mới Thuế TNCN.");

    private static async Task<IResult> UpdateManualValueAsync(
        [FromBody] UpdatePayrollPersonalIncomeTaxDeductionManualValueRequest? request,
        [FromServices] IPayrollPersonalIncomeTaxDeductionManualAdjustmentService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(request, service.UpdateManualValueAsync, AuditActions.PersonalIncomeTaxDeduction.ManualValueUpdated,
            auditScope, correlationAccessor, context, cancellationToken, "Thiếu payload điều chỉnh Thuế TNCN.");

    private static async Task<IResult> SetLockStateBatchAsync(
        [FromBody] SetPayrollPersonalIncomeTaxDeductionBatchLockStateRequest? request,
        [FromServices] IPayrollPersonalIncomeTaxDeductionLockService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken) =>
        await ExecuteAsync(request, service.SetLockStateBatchAsync, AuditActions.PersonalIncomeTaxDeduction.BatchLockStateChanged,
            auditScope, correlationAccessor, context, cancellationToken, "Thiếu payload khóa/mở khóa Thuế TNCN.");

    private static async Task<IResult> ExecuteAsync<TRequest, TResult>(
        TRequest? request,
        Func<TRequest, CancellationToken, Task<TResult>> execute,
        string action,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        HttpContext context,
        CancellationToken cancellationToken,
        string missingMessage)
        where TRequest : class
    {
        if (request is null) return Results.BadRequest(new { message = missingMessage });
        using var lease = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(), action, PayrollEndpoints.CreateAuditActor(context.User),
            correlationAccessor.Current ?? context.TraceIdentifier, AuditCaptureMode.OperationOnly));
        try { return Results.Ok(await execute(request, cancellationToken)); }
        catch (PayrollPersonalIncomeTaxDeductionConflictException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
