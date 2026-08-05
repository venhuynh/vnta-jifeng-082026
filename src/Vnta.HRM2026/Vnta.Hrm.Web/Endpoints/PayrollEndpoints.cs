using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.Common.Security;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Application.KhauTru.GiamTruGiaCanh;
using Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruBHXHYT;
using Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruPhiCongDoan;
using Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruThueTNCN;
using Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruKhac;
using Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruTongKet;
using Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapChuyenCan;
using Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiem;
using Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiemGanNhanVien;
using Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiemKhac;

namespace Vnta.Hrm.Web.Endpoints;

public static partial class PayrollEndpoints
{
    public static IEndpointRouteBuilder MapPayrollEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var payrollGroup = endpoints.MapGroup("/api/payroll")
            .WithTags("Payroll")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        payrollGroup.MapLuongCanBanEndpoints();

        payrollGroup.MapPhuCapThamNienEndpoints();
        payrollGroup.MapPhuCapKhacEndpoints();
        payrollGroup.MapKhauTruKhacEndpoints();
        payrollGroup.MapPhuCapTrachNhiemEndpoints();
        payrollGroup.MapPhuCapTrachNhiemGanNhanVienXemEndpoints();
        payrollGroup.MapBangCongTongHopEndpoints();

        payrollGroup.MapPhuCapChuyenCanEndpoints();

        #region Tổng hợp phụ cấp

        // Các API tổng hợp phụ cấp kế thừa PayrollAdministration từ payrollGroup.
        payrollGroup.MapPhuCapTongHopEndpoints();

        #endregion

        payrollGroup.MapLeaveHolidayAllowanceEndpoints();
        payrollGroup.MapPhuCapTrachNhiemKhacEndpoints();
        payrollGroup.MapKhauTruDashboardEndpoints();
        payrollGroup.MapKhauTruTongKetEndpoints();
        payrollGroup.MapKhauTruPhiCongDoanEndpoints();
        payrollGroup.MapGet("/tax-dependents/{employeeId:guid}", GetEmployeeTaxDependentsAsync);
        payrollGroup.MapPost("/tax-dependents/search", SearchEmployeeTaxDependentsAsync);
        payrollGroup.MapPost("/tax-dependents", SaveEmployeeTaxDependentAsync);
        payrollGroup.MapKhauTruBHXHYTEndpoints();
        payrollGroup.MapKhauTruThueTNCNEndpoints();

        payrollGroup.MapPhuCapComEndpoints();

        payrollGroup.MapPhuCapDocHaiEndpoints();

        endpoints.MapResponsibilityPositionAssignmentEndpoints();

        return endpoints;
    }

    #region Responsibility Allowance Configuration Endpoints

    private static async Task<IResult> GetResponsibilityAllowanceGradeConfigAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.GetGradeConfigAsync(year, month, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SaveResponsibilityAllowanceGradeAsync(
        [FromBody] SavePayrollResponsibilityAllowanceGradeRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationWriteService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload cấp bậc trách nhiệm." });
        }

        try
        {
            var result = await service.SaveGradeAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SaveResponsibilityAllowanceMappingAsync(
        [FromBody] SavePayrollResponsibilityAllowanceGradePositionRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationWriteService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload gán chức vụ vào cấp bậc." });
        }

        try
        {
            var result = await service.SaveMappingAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeactivateResponsibilityAllowanceMappingAsync(
        Guid id,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationWriteService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.DeactivateMappingAsync(id, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SaveResponsibilityAllowanceEmployeeAssignmentAsync(
        [FromBody] SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload gán trách nhiệm theo nhân viên." });
        }

        try
        {
            var result = await service.SaveEmployeeAssignmentAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ApplyResponsibilityAllowancePositionDefaultsAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload áp dụng mặc định theo chức vụ." });
        }

        try
        {
            var result = await service.ApplyPositionDefaultsToEmployeeAssignmentsAsync(request.Year, request.Month, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    #endregion

    #region Responsibility Allowance Monthly ABC Endpoints

    private static async Task<IResult> GetMonthlyResponsibilityAllowanceAbcAsync(
        [FromQuery] int year,
        [FromQuery] int month,
        [FromQuery] bool? isLocked,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.GetAbcAsync(new PayrollResponsibilityAllowanceAbcFilter(year, month, isLocked), cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }


    private static async Task<IResult> RefreshMonthlyResponsibilityAllowanceAbcAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcRefreshService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload làm mới bảng ABC trách nhiệm." });
        }

        try
        {
            var result = await service.RefreshAbcAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> CalculateMonthlyResponsibilityAllowanceAbcAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcRefreshService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload tính ABC trách nhiệm." });
        }

        try
        {
            var result = await service.CalculateAbcAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RecalculateMonthlyResponsibilityAllowanceAbcAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceRecalculationService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload tính lại bảng ABC trách nhiệm." });
        }

        try
        {
            var result = await service.RecalculateAbcAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> CopyMonthlyResponsibilityAllowanceAbcFromPreviousAsync(
        int year,
        int month,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcCopyService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CopyAbcFromPreviousMonthAsync(year, month, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> LockMonthlyResponsibilityAllowanceAbcRowAsync(
        Guid employeeId,
        int year,
        int month,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcLockService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SetLockStateAsync(employeeId, year, month, true, null, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UnlockMonthlyResponsibilityAllowanceAbcRowAsync(
        Guid employeeId,
        int year,
        int month,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcLockService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.SetLockStateAsync(employeeId, year, month, false, null, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetMonthlyResponsibilityAllowanceAbcBatchLockStateAsync(
        [FromBody] SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu thông tin khóa hoặc mở khóa phụ cấp trách nhiệm." });
        }

        try
        {
            using var auditLease = auditScope.Begin(new AuditCommand(
                Guid.NewGuid(),
                AuditActions.ResponsibilityAllowance.BatchLockStateChanged,
                CreateAuditActor(httpContext.User),
                correlationAccessor.Current ?? httpContext.TraceIdentifier,
                AuditCaptureMode.OperationOnly));
            var result = await service.SetLockStateBatchAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SaveMonthlyResponsibilityAllowanceAdjustmentAsync(
        [FromBody] SavePayrollResponsibilityAllowanceAdjustmentRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload điều chỉnh phụ cấp trách nhiệm." });
        }

        try
        {
            var result = await service.SaveAdjustmentAsync(request, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetMonthlyResponsibilityAllowanceUpdateContextAsync(
        [FromQuery] Guid employeeId,
        [FromQuery] int year,
        [FromQuery] int month,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcQueryService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.GetUpdateContextAsync(employeeId, year, month, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateMonthlyResponsibilityAllowancePerformanceBonusAsync(
        Guid employeeId,
        int year,
        int month,
        [FromBody] UpdatePayrollResponsibilityPerformanceBonusPayload? payload,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService service,
        CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload thưởng hiệu suất." });
        }

        try
        {
            var result = await service.UpdatePerformanceBonusAsync(employeeId, year, month, payload.MonthlyPerformanceBonusAmount, null, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateMonthlyResponsibilityAllowancePerformanceBonusExclusionAsync(
        Guid employeeId,
        int year,
        int month,
        [FromBody] UpdatePayrollResponsibilityPerformanceBonusExclusionPayload? payload,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService service,
        CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload trạng thái áp dụng thưởng hiệu suất." });
        }

        try
        {
            var result = await service.UpdatePerformanceBonusExclusionAsync(employeeId, year, month, payload.IsPerformanceBonusExcluded, null, cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateMonthlyResponsibilityAllowancePerformanceBonusForPeriodAsync(
        int year,
        int month,
        [FromBody] UpdatePayrollResponsibilityPerformanceBonusPayload? payload,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService service,
        CancellationToken cancellationToken)
    {
        if (payload is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload thưởng hiệu suất theo kỳ." });
        }

        try
        {
            var result = await service.UpdatePerformanceBonusForPeriodAsync(
                year,
                month,
                payload.MonthlyPerformanceBonusAmount,
                payload.ConcurrencyTokens,
                cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    #endregion


    private static async Task<IResult> SearchLeaveHolidayAllowanceAsync(
        [FromBody] LeaveHolidayAllowanceFilter? filter,
        [FromServices] ILeaveHolidayAllowanceReadService leaveHolidayAllowanceService,
        CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var result = await leaveHolidayAllowanceService.SearchAsync(
            filter ?? new LeaveHolidayAllowanceFilter(today.Month, today.Year, null),
            cancellationToken);

        return Results.Ok(result);
    }

    private static async Task<IResult> ClearLeaveHolidayAllowanceManualValuesAsync(
        [FromBody] ClearLeaveHolidayAllowanceManualValuesRequest? request,
        [FromServices] ILeaveHolidayAllowanceClearManualValuesService leaveHolidayAllowanceService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload xóa dữ liệu nhập tay phụ cấp Phép - Lễ." });
        }

        try
        {
            var result = await leaveHolidayAllowanceService.ClearManualValuesAsync(
                request with { Actor = ResolveAuditActor(httpContext.User) },
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SyncLeaveHolidayAllowanceFromPreviousMonthAsync(
        [FromBody] SyncLeaveHolidayAllowanceFromPreviousMonthRequest? request,
        [FromServices] ILeaveHolidayAllowancePreviousMonthSyncService leaveHolidayAllowanceService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload đồng bộ phụ cấp Phép - Lễ từ tháng trước." });
        }

        try
        {
            var result = await leaveHolidayAllowanceService.SyncFromPreviousMonthAsync(
                request with { Actor = ResolveAuditActor(httpContext.User) },
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RecalculateLeaveHolidayAllowanceAsync(
        [FromBody] RecalculateLeaveHolidayAllowanceRequest? request,
        [FromServices] ILeaveHolidayAllowanceRecalculationService leaveHolidayAllowanceService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload tính lại phụ cấp Phép - Lễ." });
        }

        try
        {
            var result = await leaveHolidayAllowanceService.RecalculateAsync(
                request with { Actor = ResolveAuditActor(httpContext.User) },
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateLeaveHolidayAllowanceManualValuesAsync(
        [FromBody] UpdateLeaveHolidayAllowanceManualValuesRequest? request,
        [FromServices] ILeaveHolidayAllowanceManualAdjustmentService leaveHolidayAllowanceService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload cập nhật phụ cấp Phép - Lễ." });
        }

        try
        {
            var result = await leaveHolidayAllowanceService.UpdateManualValuesAsync(
                request with { Actor = ResolveAuditActor(httpContext.User) },
                cancellationToken);
            return Results.Ok(result);
        }
        catch(LeaveHolidayAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetLeaveHolidayAllowanceLockStateAsync(
        [FromBody] SetLeaveHolidayAllowanceLockStateRequest? request,
        [FromServices] ILeaveHolidayAllowanceLockService leaveHolidayAllowanceService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa phụ cấp Phép - Lễ." });
        }

        try
        {
            var result = await leaveHolidayAllowanceService.SetLockStateAsync(
                request with { Actor = ResolveAuditActor(httpContext.User) },
                cancellationToken);
            return Results.Ok(result);
        }
        catch(LeaveHolidayAllowanceConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetLeaveHolidayAllowanceBatchLockStateAsync(
        [FromBody] SetLeaveHolidayAllowanceBatchLockStateRequest? request,
        [FromServices] ILeaveHolidayAllowanceLockService leaveHolidayAllowanceService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa hàng loạt phụ cấp Phép - Lễ." });
        }

        try
        {
            var result = await leaveHolidayAllowanceService.SetLockStateBatchAsync(
                request with { Actor = ResolveAuditActor(httpContext.User) },
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetPayrollUnionFeeDeductionLockStateAsync(
        HttpContext httpContext,
        [FromBody] SetPayrollUnionFeeDeductionLockStateRequest? request,
        [FromServices] IPayrollUnionFeeDeductionLockService lockService,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa phí công đoàn." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.UnionFeeDeduction.SetLockState,
                token => lockService.SetLockStateAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdatePayrollUnionFeeDeductionManualValueAsync(
        HttpContext httpContext,
        [FromBody] UpdatePayrollUnionFeeDeductionManualValueRequest? request,
        [FromServices] IPayrollUnionFeeDeductionManualAdjustmentService manualAdjustmentService,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu dữ liệu điều chỉnh phí công đoàn." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.UnionFeeDeduction.ManualValueUpdated,
                token => manualAdjustmentService.UpdateManualValueAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(PayrollUnionFeeDeductionConflictException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SetPayrollUnionFeeDeductionBatchLockStateAsync(
        HttpContext httpContext,
        [FromBody] SetPayrollUnionFeeDeductionBatchLockStateRequest? request,
        [FromServices] IPayrollUnionFeeDeductionLockService lockService,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload khóa hoặc mở khóa hàng loạt phí công đoàn." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.UnionFeeDeduction.SetLockStateBatch,
                token => lockService.SetLockStateBatchAsync(request, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetEmployeeTaxDependentsAsync(
        Guid employeeId,
        [FromServices] IEmployeeTaxDependentService taxDependentService,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await taxDependentService.GetByEmployeeAsync(employeeId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SearchEmployeeTaxDependentsAsync(
        [FromBody] EmployeeTaxDependentFilter? filter,
        [FromServices] IEmployeeTaxDependentService taxDependentService,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await taxDependentService.SearchAsync(
                filter ?? new EmployeeTaxDependentFilter(null, null),
                cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SaveEmployeeTaxDependentAsync(
        [FromBody] SaveEmployeeTaxDependentRequest? request,
        [FromServices] IEmployeeTaxDependentService taxDependentService,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu payload hồ sơ người phụ thuộc." });
        }

        try
        {
            var result = await ExecuteAuditedPayrollCommandAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.EmployeeTaxDependent.Saved,
                token => taxDependentService.SaveAsync(
                    request with { Actor = ResolveAuditActor(httpContext.User) }, token),
                cancellationToken);
            return Results.Ok(result);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<T> ExecuteAuditedPayrollCommandAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        string actionIntent,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        using var auditLease = auditScope.Begin(new AuditCommand(
            Guid.NewGuid(),
            actionIntent,
            CreateAuditActor(httpContext.User),
            correlationAccessor.Current ?? httpContext.TraceIdentifier,
            AuditCaptureMode.EntityChanges));
        return await action(cancellationToken);
    }

    internal static string ResolveAuditActor(ClaimsPrincipal user) =>
        NormalizeAuditActor(user.FindFirst(ClaimTypes.Email)?.Value)
        ?? NormalizeAuditActor(user.FindFirst(ClaimTypes.Name)?.Value)
        ?? NormalizeAuditActor(user.FindFirst(ClaimTypes.NameIdentifier)?.Value)
        ?? "authenticated-user";

    internal static AuditActor CreateAuditActor(ClaimsPrincipal user)
    {
        var actorId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new UnauthorizedAccessException("Không xác định được người dùng thực hiện thao tác.");
        }

        var displayName = user.FindFirstValue(ClaimTypes.Name)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? user.Identity?.Name
            ?? actorId;
        return new AuditActor(actorId, displayName, AuditActorKind.User, AuditSource.Api);
    }

    private static string? NormalizeAuditActor(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record UpdatePayrollResponsibilityPerformanceBonusPayload(
        decimal MonthlyPerformanceBonusAmount,
        IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? ConcurrencyTokens = null);

    private sealed record UpdatePayrollResponsibilityPerformanceBonusExclusionPayload(bool IsPerformanceBonusExcluded);
}
