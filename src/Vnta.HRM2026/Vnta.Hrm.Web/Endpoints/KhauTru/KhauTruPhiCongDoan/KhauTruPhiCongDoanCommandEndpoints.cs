using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruPhiCongDoan;

internal static class KhauTruPhiCongDoanCommandEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/union-fee-deductions/prepare-period", PreparePeriodAsync);
        endpoints.MapPost("/union-fee-deductions/refresh", RefreshAsync);
        endpoints.MapPost("/union-fee-deductions/manual-value", UpdateManualValueAsync);
        endpoints.MapPost("/union-fee-deductions/lock-state", SetLockStateAsync);
        endpoints.MapPost("/union-fee-deductions/lock-state/batch", SetBatchLockStateAsync);
    }

    private static async Task<IResult> PreparePeriodAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IPayrollUnionFeeDeductionPeriodPreparationService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(httpContext, auditScope, correlationAccessor,
                AuditActions.UnionFeeDeduction.PeriodPrepared,
                token => service.PreparePeriodAsync(year, month, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshPayrollUnionFeeDeductionRequest? request,
        [FromServices] IPayrollUnionFeeDeductionRefreshService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload tính lại phí công đoàn." });
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(httpContext, auditScope, correlationAccessor,
                AuditActions.UnionFeeDeduction.Refreshed,
                token => service.RefreshAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> UpdateManualValueAsync(
        [FromBody] UpdatePayrollUnionFeeDeductionManualValueRequest? request,
        [FromServices] IPayrollUnionFeeDeductionManualAdjustmentService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu dữ liệu điều chỉnh phí công đoàn." });
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(httpContext, auditScope, correlationAccessor,
                AuditActions.UnionFeeDeduction.ManualValueUpdated,
                token => service.UpdateManualValueAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (PayrollUnionFeeDeductionConflictException ex) { return Results.Conflict(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> SetLockStateAsync(
        [FromBody] SetPayrollUnionFeeDeductionLockStateRequest? request,
        [FromServices] IPayrollUnionFeeDeductionLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa phí công đoàn." });
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(httpContext, auditScope, correlationAccessor,
                AuditActions.UnionFeeDeduction.SetLockState,
                token => service.SetLockStateAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> SetBatchLockStateAsync(
        [FromBody] SetPayrollUnionFeeDeductionBatchLockStateRequest? request,
        [FromServices] IPayrollUnionFeeDeductionLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null) return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa hàng loạt phí công đoàn." });
        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(httpContext, auditScope, correlationAccessor,
                AuditActions.UnionFeeDeduction.SetLockStateBatch,
                token => service.SetLockStateBatchAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
