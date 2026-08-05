using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiem;

internal static class ResponsibilityAllowanceCommandEndpoints
{
    internal static Task<IResult> SaveGradeAsync(
        [FromBody] SavePayrollResponsibilityAllowanceGradeRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationWriteService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload cấp bậc trách nhiệm."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.SaveGradeAsync(request, token), cancellationToken);

    internal static Task<IResult> SaveMappingAsync(
        [FromBody] SavePayrollResponsibilityAllowanceGradePositionRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationWriteService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload gán chức vụ và cấp bậc."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.SaveMappingAsync(request, token), cancellationToken);

    internal static Task<IResult> DeactivateMappingAsync(
        Guid id,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationWriteService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.DeactivateMappingAsync(id, token), cancellationToken);

    internal static Task<IResult> CopyConfigurationFromPreviousMonthAsync(
        [FromBody] CopyConfigurationPayload? request,
        [FromServices] IPayrollResponsibilityAllowanceGradeConfigurationWriteService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu kỳ cần sao chép cấu hình."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.CopyFromPreviousMonthAsync(request.Year, request.Month, request.CopyMappings, token), cancellationToken);

    internal static Task<IResult> SaveEmployeeAssignmentAsync(
        [FromBody] SavePayrollResponsibilityAllowanceEmployeeAssignmentRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload gán trách nhiệm theo nhân viên."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.SaveEmployeeAssignmentAsync(request, token), cancellationToken);

    internal static Task<IResult> SynchronizeEmployeeAssignmentsForSummariesAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload đồng bộ danh sách nhân viên."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.EnsureEmployeeAssignmentsForSummariesAsync(request.Year, request.Month, token), cancellationToken);

    internal static Task<IResult> LoadEmployeeAssignmentsFromPreviousMonthAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload lấy dữ liệu từ tháng trước."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.LoadEmployeeAssignmentsFromPreviousMonthAsync(request.Year, request.Month, token), cancellationToken);

    internal static Task<IResult> ApplyPositionDefaultsAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload áp dụng mặc định theo chức vụ."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.ApplyPositionDefaultsToEmployeeAssignmentsAsync(request.Year, request.Month, token), cancellationToken);

    internal static Task<IResult> RecalculateEmployeeAssignmentsAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload tính lại cấp bậc nhân viên."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.RecalculateEmployeeAssignmentsAsync(request.Year, request.Month, token), cancellationToken);

    internal static Task<IResult> ExportEmployeeAssignmentsAsync(
        [FromBody] PayrollResponsibilityAllowanceEmployeeAssignmentExportRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentExportService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu yêu cầu xuất danh sách gán cấp bậc nhân viên."))
            : ResponsibilityAllowanceEndpointExecution.CommandAsync(httpContext, auditScope, correlationAccessor,
                AuditActions.ResponsibilityAllowance.Exported,
                token => service.ExportEmployeeAssignmentsAsync(request, token), cancellationToken, AuditCaptureMode.OperationOnly);

    internal static Task<IResult> UpdateAndRefreshEmployeeAssignmentAsync(
        [FromBody] UpdatePayrollResponsibilityAllowanceEmployeeAssignmentRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceEmployeeAssignmentCommandService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload cập nhật gán cấp bậc nhân viên."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.UpdateAndRefreshEmployeeAssignmentAsync(request, token), cancellationToken);

    internal static Task<IResult> ExportMonthlyAbcAsync(
        [FromBody] PayrollResponsibilityAllowanceAbcExportRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcExportService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Yêu cầu xuất phụ cấp trách nhiệm không hợp lệ."))
            : ResponsibilityAllowanceEndpointExecution.CommandAsync(httpContext, auditScope, correlationAccessor,
                AuditActions.ResponsibilityAllowance.Exported,
                token => service.ExportAsync(request, token), cancellationToken, AuditCaptureMode.OperationOnly);

    internal static Task<IResult> RefreshMonthlyAbcAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcRefreshService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload làm mới bảng ABC trách nhiệm."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.RefreshAbcAsync(request, token), cancellationToken);

    internal static Task<IResult> CalculateMonthlyAbcAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcRefreshService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload tính ABC trách nhiệm."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.CalculateAbcAsync(request, token), cancellationToken);

    internal static Task<IResult> RecalculateMonthlyAbcAsync(
        [FromBody] RefreshPayrollResponsibilityAllowanceAbcRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceRecalculationService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload tính lại bảng ABC trách nhiệm."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.RecalculateAbcAsync(request, token), cancellationToken);

    internal static Task<IResult> CopyMonthlyAbcFromPreviousAsync(
        int year,
        int month,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcCopyService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.CopyAbcFromPreviousMonthAsync(year, month, token), cancellationToken);

    internal static Task<IResult> LockMonthlyAbcAsync(
        Guid employeeId,
        int year,
        int month,
        DateTime? originalUpdatedAtUtc,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
            token => service.SetLockStateAsync(employeeId, year, month, true, originalUpdatedAtUtc, token), cancellationToken);

    internal static Task<IResult> UnlockMonthlyAbcAsync(
        Guid employeeId,
        int year,
        int month,
        DateTime? originalUpdatedAtUtc,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
            token => service.SetLockStateAsync(employeeId, year, month, false, originalUpdatedAtUtc, token), cancellationToken);

    internal static Task<IResult> SetMonthlyAbcBatchLockStateAsync(
        [FromBody] SetPayrollResponsibilityAllowanceAbcBatchLockStateRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcLockService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null
            ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu thông tin khóa hoặc mở khóa phụ cấp trách nhiệm."))
            : ResponsibilityAllowanceEndpointExecution.CommandAsync(httpContext, auditScope, correlationAccessor,
                AuditActions.ResponsibilityAllowance.BatchLockStateChanged,
                token => service.SetLockStateBatchAsync(request, token), cancellationToken);

    internal static Task<IResult> SaveMonthlyAbcAdjustmentAsync(
        [FromBody] SavePayrollResponsibilityAllowanceAdjustmentRequest? request,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcManualAdjustmentService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        request is null ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload điều chỉnh phụ cấp trách nhiệm."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor, token => service.SaveAdjustmentAsync(request, token), cancellationToken);

    internal static Task<IResult> UpdatePerformanceBonusAsync(
        Guid employeeId,
        int year,
        int month,
        [FromBody] UpdatePerformanceBonusPayload? payload,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        payload is null ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload thưởng hiệu suất."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.UpdatePerformanceBonusAsync(employeeId, year, month, payload.MonthlyPerformanceBonusAmount, payload.OriginalUpdatedAtUtc, token), cancellationToken);

    internal static Task<IResult> UpdatePerformanceBonusExclusionAsync(
        Guid employeeId,
        int year,
        int month,
        [FromBody] UpdatePerformanceBonusExclusionPayload? payload,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        payload is null ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload trạng thái áp dụng thưởng hiệu suất."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.UpdatePerformanceBonusExclusionAsync(employeeId, year, month, payload.IsPerformanceBonusExcluded, payload.OriginalUpdatedAtUtc, token), cancellationToken);

    internal static Task<IResult> UpdatePerformanceBonusForPeriodAsync(
        int year,
        int month,
        [FromBody] UpdatePerformanceBonusPayload? payload,
        [FromServices] IPayrollResponsibilityAllowanceMonthlyAbcPerformanceBonusService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        payload is null ? Task.FromResult(ResponsibilityAllowanceEndpointExecution.MissingPayload("Thiếu payload thưởng hiệu suất theo kỳ."))
            : ExecuteMutationAsync(httpContext, auditScope, correlationAccessor,
                token => service.UpdatePerformanceBonusForPeriodAsync(year, month, payload.MonthlyPerformanceBonusAmount, payload.ConcurrencyTokens, token), cancellationToken);

    private static Task<IResult> ExecuteMutationAsync<T>(
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken) =>
        ResponsibilityAllowanceEndpointExecution.CommandAsync(
            httpContext,
            auditScope,
            correlationAccessor,
            AuditActions.ResponsibilityAllowance.Mutation,
            operation,
            cancellationToken);

    internal sealed record CopyConfigurationPayload(int Year, int Month, bool CopyMappings);

    internal sealed record UpdatePerformanceBonusPayload(
        decimal MonthlyPerformanceBonusAmount,
        DateTime? OriginalUpdatedAtUtc = null,
        IReadOnlyList<PayrollResponsibilityAllowanceAbcConcurrencyToken>? ConcurrencyTokens = null);

    internal sealed record UpdatePerformanceBonusExclusionPayload(
        bool IsPerformanceBonusExcluded,
        DateTime? OriginalUpdatedAtUtc = null);
}
