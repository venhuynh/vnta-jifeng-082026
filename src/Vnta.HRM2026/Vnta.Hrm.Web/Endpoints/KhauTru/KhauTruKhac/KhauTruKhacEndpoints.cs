using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruKhac;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruKhac;

/// <summary>
/// HTTP boundary riêng cho Khấu trừ khác. Giữ route cũ để không làm vỡ typed client,
/// đồng thời tách feature khỏi điểm tập trung PayrollEndpoints.
/// </summary>
public static class KhauTruKhacEndpoints
{
    public static RouteGroupBuilder MapKhauTruKhacEndpoints(this RouteGroupBuilder payrollGroup)
    {
        payrollGroup.MapPost("/other-deductions/prepare-period", PreparePeriodAsync);
        payrollGroup.MapPost("/other-deductions/search", SearchAsync);
        payrollGroup.MapPost("/other-deductions/search-page", SearchPageAsync);
        payrollGroup.MapPost("/other-deductions/refresh", RefreshAsync);
        payrollGroup.MapPost("/other-deductions/manual-values", UpdateManualValuesAsync);
        payrollGroup.MapPost("/other-deductions/lock-state", SetLockStateAsync);
        payrollGroup.MapPost("/other-deductions/lock-state/batch", SetBatchLockStateAsync);

        return payrollGroup;
    }

    private static async Task<IResult> PreparePeriodAsync(
        [FromBody] PreparePayrollEmployeeOtherDeductionAllowancePeriodRequest? request,
        [FromServices] IPayrollEmployeeOtherDeductionAllowanceService service,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu kỳ lương khấu trừ khác." });
        }

        try
        {
            await service.PreparePeriodAsync(request.PayrollYear, request.PayrollMonth, cancellationToken);
            return Results.NoContent();
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SearchAsync(
        [FromBody] PayrollEmployeeOtherDeductionAllowanceFilter? filter,
        [FromServices] IPayrollEmployeeOtherDeductionAllowanceService service,
        CancellationToken cancellationToken)
    {
        if(filter is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện tìm kiếm khấu trừ khác." });
        }

        try
        {
            return Results.Ok(await service.SearchAsync(filter, cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SearchPageAsync(
        [FromBody] PayrollEmployeeOtherDeductionAllowanceFilter? filter,
        [FromServices] IPayrollEmployeeOtherDeductionAllowanceService service,
        CancellationToken cancellationToken)
    {
        if(filter is null)
        {
            return Results.BadRequest(new { message = "Thiếu điều kiện tìm kiếm khấu trừ khác." });
        }

        try
        {
            return Results.Ok(await service.SearchPageAsync(filter, cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshPayrollEmployeeOtherDeductionAllowanceRequest? request,
        [FromServices] IPayrollEmployeeOtherDeductionAllowanceService service,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu yêu cầu làm mới khấu trừ khác." });
        }

        try
        {
            return Results.Ok(await service.RefreshAsync(request, cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateManualValuesAsync(
        HttpContext httpContext,
        [FromBody] UpdatePayrollEmployeeOtherDeductionAllowanceManualValuesRequest? request,
        [FromServices] IPayrollEmployeeOtherDeductionAllowanceService service,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu dữ liệu điều chỉnh khấu trừ khác." });
        }

        try
        {
            var result = await ExecuteAuditedAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.OtherDeduction.ManualValueUpdated,
                token => service.UpdateManualValuesAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(PayrollEmployeeOtherDeductionConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetLockStateAsync(
        [FromBody] SetPayrollEmployeeOtherDeductionAllowanceLockStateRequest? request,
        [FromServices] IPayrollEmployeeOtherDeductionAllowanceService service,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu yêu cầu cập nhật trạng thái khóa." });
        }

        try
        {
            return Results.Ok(await service.SetLockStateAsync(request, cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetBatchLockStateAsync(
        [FromBody] SetPayrollEmployeeOtherDeductionAllowanceBatchLockStateRequest? request,
        [FromServices] IPayrollEmployeeOtherDeductionAllowanceService service,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu yêu cầu cập nhật trạng thái khóa hàng loạt." });
        }

        try
        {
            return Results.Ok(await service.SetLockStateBatchAsync(request, cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<T> ExecuteAuditedAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string actionIntent,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        using var scope = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            actionIntent,
            CreateAuditActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            AuditCaptureMode.EntityChanges));
        return await action(cancellationToken);
    }

    private static AuditActor CreateAuditActor(ClaimsPrincipal user)
    {
        var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if(string.IsNullOrWhiteSpace(actorId))
        {
            throw new UnauthorizedAccessException("Không xác định được người dùng thực hiện thao tác.");
        }

        var displayName = user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? actorId;
        return new AuditActor(actorId, displayName, AuditActorKind.User, AuditSource.Api);
    }
}
