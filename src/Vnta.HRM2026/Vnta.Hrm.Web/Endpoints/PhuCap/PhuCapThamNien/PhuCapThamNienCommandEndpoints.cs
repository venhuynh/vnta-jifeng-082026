using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>
/// HTTP boundary for seniority allowance operations. The payroll group owns the
/// authorization policy; this feature owns only its route contracts.
/// </summary>
public static partial class PayrollEndpoints
{
    private static async Task<IResult> PrepareSeniorityAllowancePeriodAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IPayrollEmployeeSeniorityAllowancePeriodPreparationService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        try
        {
            await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, "SeniorityAllowance.PreparePeriod",
                token => service.PreparePeriodAsync(year, month, token), cancellationToken);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RefreshSeniorityAllowancesAsync(
        [FromBody] RefreshPayrollEmployeeSeniorityAllowanceRequest? request,
        [FromServices] IPayrollEmployeeSeniorityAllowanceRefreshService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload làm mới phụ cấp thâm niên." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, "SeniorityAllowance.Refresh",
                token => service.RefreshAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetSeniorityAllowanceLockStateAsync(
        [FromBody] SetPayrollEmployeeSeniorityAllowanceLockStateRequest? request,
        [FromServices] IPayrollEmployeeSeniorityAllowanceLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa phụ cấp thâm niên." });
        }

        if (request.OriginalUpdatedAtUtc == default)
        {
            return Results.Conflict(new { message = "Thiếu phiên bản dữ liệu gốc. Vui lòng tải lại dữ liệu trước khi thay đổi trạng thái khóa." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, AuditActions.SeniorityAllowance.LockStateChanged,
                token => service.SetLockStateAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (PayrollEmployeeSeniorityAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetSeniorityAllowanceBatchLockStateAsync(
        [FromBody] SetPayrollEmployeeSeniorityAllowanceBatchLockStateRequest? request,
        [FromServices] IPayrollEmployeeSeniorityAllowanceLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa hàng loạt phụ cấp thâm niên." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, AuditActions.SeniorityAllowance.BatchLockStateChanged,
                token => service.SetLockStateBatchAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateSeniorityAllowanceManualValuesAsync(
        [FromBody] UpdatePayrollEmployeeSeniorityAllowanceManualValuesRequest? request,
        [FromServices] IPayrollEmployeeSeniorityAllowanceManualAdjustmentService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload cập nhật thủ công phụ cấp thâm niên." });
        }

        if (request.OriginalUpdatedAtUtc == default)
        {
            return Results.Conflict(new { message = "Thiếu phiên bản dữ liệu gốc. Vui lòng tải lại dữ liệu trước khi cập nhật." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, AuditActions.SeniorityAllowance.ManualValueUpdated,
                token => service.UpdateManualValuesAsync(request, token), cancellationToken);
            return Results.Ok(result);
        }
        catch (PayrollEmployeeSeniorityAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

}
