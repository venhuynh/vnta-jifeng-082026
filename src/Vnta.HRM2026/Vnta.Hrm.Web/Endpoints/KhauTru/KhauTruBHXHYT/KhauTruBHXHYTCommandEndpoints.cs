using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruBHXHYT;

internal static class KhauTruBHXHYTCommandEndpoints
{
    internal static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/social-health-insurance-deductions/refresh", RefreshPayrollInsuranceDeductionsAsync);
        endpoints.MapPost("/social-health-insurance-deductions/sync-previous-month", SyncPayrollInsuranceDeductionsFromPreviousMonthAsync);
        endpoints.MapPost("/social-health-insurance-deductions/manual-values", UpdatePayrollInsuranceDeductionManualValuesAsync);
        endpoints.MapPost("/social-health-insurance-deductions/lock-state", SetPayrollInsuranceDeductionLockStateAsync);
        endpoints.MapPost("/social-health-insurance-deductions/lock-state/batch", SetPayrollInsuranceDeductionBatchLockStateAsync);
        endpoints.MapPost("/social-health-insurance-deductions/validate", ValidatePayrollInsuranceDeductionAsync);
        endpoints.MapPost("/social-health-insurance-deductions", SavePayrollInsuranceDeductionAsync);
        endpoints.MapPost("/social-health-insurance-deductions/delete", DeletePayrollInsuranceDeductionsAsync);
    }

    private static async Task<IResult> RefreshPayrollInsuranceDeductionsAsync(
        [FromBody] RefreshPayrollInsuranceDeductionRequest? request,
        [FromServices] IPayrollInsuranceDeductionRefreshService payrollInsuranceDeductionService,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload tính lại khấu trừ BHXH-YT." });
        }

        try
        {
            using var auditLease = auditScope.Begin(new AuditCommand(
                Guid.NewGuid(),
                AuditActions.PayrollInsuranceDeduction.Refresh,
                PayrollEndpoints.CreateAuditActor(httpContext.User),
                correlationAccessor.Current ?? httpContext.TraceIdentifier,
                AuditCaptureMode.EntityChanges));
            var result = await payrollInsuranceDeductionService.RefreshAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SyncPayrollInsuranceDeductionsFromPreviousMonthAsync(
        [FromBody] SyncPayrollInsuranceDeductionFromPreviousMonthRequest? request,
        [FromServices] IPayrollInsuranceDeductionPreviousMonthSyncService payrollInsuranceDeductionService,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khấu trừ BHXH-YT." });
        }

        try
        {
            using var auditLease = auditScope.Begin(new AuditCommand(
                Guid.NewGuid(),
                AuditActions.PayrollInsuranceDeduction.SyncedFromPreviousMonth,
                PayrollEndpoints.CreateAuditActor(httpContext.User),
                correlationAccessor.Current ?? httpContext.TraceIdentifier,
                AuditCaptureMode.EntityChanges));
            var result = await payrollInsuranceDeductionService.SyncFromPreviousMonthAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ValidatePayrollInsuranceDeductionAsync(
        [FromBody] UpsertPayrollInsuranceDeductionRequest? request,
        [FromServices] IPayrollInsuranceDeductionLegacyWriteService payrollInsuranceDeductionService,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khấu trừ BHXH-YT." });
        }

        var validationMessage = await payrollInsuranceDeductionService.ValidateAsync(request, cancellationToken);
        return Results.Ok(validationMessage);
    }

    private static async Task<IResult> UpdatePayrollInsuranceDeductionManualValuesAsync(
        [FromBody] UpdatePayrollInsuranceDeductionManualValuesRequest? request,
        [FromServices] IPayrollInsuranceDeductionManualAdjustmentService payrollInsuranceDeductionService,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload điều chỉnh khấu trừ BHXH-YT." });
        }

        using var auditLease = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            AuditActions.PayrollInsuranceDeduction.ManualValuesUpdated,
            PayrollEndpoints.CreateAuditActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            AuditCaptureMode.OperationOnly));

        try
        {
            var result = await payrollInsuranceDeductionService.UpdateManualValuesAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (PayrollInsuranceDeductionConcurrencyException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetPayrollInsuranceDeductionLockStateAsync(
        [FromBody] SetPayrollInsuranceDeductionLockStateRequest? request,
        [FromServices] IPayrollInsuranceDeductionLockService payrollInsuranceDeductionService,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa khấu trừ BHXH-YT." });
        }

        using var auditLease = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            AuditActions.PayrollInsuranceDeduction.LockStateChanged,
            PayrollEndpoints.CreateAuditActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            AuditCaptureMode.OperationOnly));

        try
        {
            var result = await payrollInsuranceDeductionService.SetLockStateAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (PayrollInsuranceDeductionConcurrencyException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetPayrollInsuranceDeductionBatchLockStateAsync(
        [FromBody] SetPayrollInsuranceDeductionBatchLockStateRequest? request,
        [FromServices] IPayrollInsuranceDeductionLockService payrollInsuranceDeductionService,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa hàng loạt khấu trừ BHXH-YT." });
        }

        using var auditLease = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            AuditActions.PayrollInsuranceDeduction.BatchLockStateChanged,
            PayrollEndpoints.CreateAuditActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            AuditCaptureMode.OperationOnly,
            Metadata: new Dictionary<string, string>
            {
                ["scope"] = request.PayrollDeductionSummaryRecordIds is null ? "whole-period" : "selected-rows",
                ["payrollPeriod"] = $"{request.PayrollMonth:00}/{request.PayrollYear}",
                ["isLocked"] = request.IsLocked.ToString()
            }));

        try
        {
            var result = await payrollInsuranceDeductionService.SetLockStateBatchAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (PayrollInsuranceDeductionConcurrencyException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SavePayrollInsuranceDeductionAsync(
        [FromQuery] bool isNew,
        [FromBody] UpsertPayrollInsuranceDeductionRequest? request,
        [FromServices] IPayrollInsuranceDeductionLegacyWriteService payrollInsuranceDeductionService,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khấu trừ BHXH-YT." });
        }

        if (!isNew)
        {
            return Results.BadRequest(new { message = "Điều chỉnh khấu trừ BHXH-YT phải dùng command điều chỉnh thủ công." });
        }

        try
        {
            using var auditLease = auditScope.Begin(new AuditCommand(
                Guid.NewGuid(),
                AuditActions.PayrollInsuranceDeduction.Created,
                PayrollEndpoints.CreateAuditActor(httpContext.User),
                correlationAccessor.Current ?? httpContext.TraceIdentifier,
                AuditCaptureMode.EntityChanges));
            var result = await payrollInsuranceDeductionService.SaveAsync(request, isNew, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeletePayrollInsuranceDeductionsAsync(
        [FromBody] IReadOnlyCollection<Guid>? ids,
        [FromServices] IPayrollInsuranceDeductionLegacyWriteService payrollInsuranceDeductionService,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (ids is null)
        {
            return Results.BadRequest(new { message = "Thiếu danh sách dòng khấu trừ BHXH-YT cần xóa." });
        }

        using var auditLease = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            AuditActions.PayrollInsuranceDeduction.Deleted,
            PayrollEndpoints.CreateAuditActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            AuditCaptureMode.EntityChanges));
        await payrollInsuranceDeductionService.DeleteAsync(ids, cancellationToken);
        return Results.NoContent();
    }
}
