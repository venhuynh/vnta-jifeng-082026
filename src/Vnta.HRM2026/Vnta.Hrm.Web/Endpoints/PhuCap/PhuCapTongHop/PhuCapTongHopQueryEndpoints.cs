using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Query endpoints owned by the allowance-summary feature.</summary>
internal static class PhuCapTongHopQueryEndpoints
{
    internal static Task<IResult> GetOverviewAsync(
        [FromBody] PayrollAllowanceSummaryFilter? filter,
        [FromServices] IPayrollAllowanceSummaryReadService service,
        CancellationToken cancellationToken) =>
        GetOverviewCoreAsync(filter, service, cancellationToken);

    internal static Task<IResult> SearchAsync(
        [FromBody] PayrollAllowanceSummaryFilter? filter,
        [FromServices] IPayrollAllowanceSummaryReadService service,
        CancellationToken cancellationToken) =>
        SearchCoreAsync(filter, service, cancellationToken);

    internal static Task<IResult> ExportAsync(
        [FromBody] PayrollAllowanceSummaryExportRequest? request,
        [FromServices] IPayrollAllowanceSummaryExportService service,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken) =>
        ExportCoreAsync(request, service, httpContext, auditScope, correlationAccessor, cancellationToken);

    private static async Task<IResult> GetOverviewCoreAsync(
        PayrollAllowanceSummaryFilter? filter,
        IPayrollAllowanceSummaryReadService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.GetSummaryAsync(
            filter ?? new PayrollAllowanceSummaryFilter(null, null, null), cancellationToken));

    private static async Task<IResult> SearchCoreAsync(
        PayrollAllowanceSummaryFilter? filter,
        IPayrollAllowanceSummaryReadService service,
        CancellationToken cancellationToken) =>
        Results.Ok(await service.SearchAsync(
            filter ?? new PayrollAllowanceSummaryFilter(null, null, null), cancellationToken));

    private static async Task<IResult> ExportCoreAsync(
        PayrollAllowanceSummaryExportRequest? request,
        IPayrollAllowanceSummaryExportService service,
        HttpContext httpContext,
        IAuditScope auditScope,
        IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Results.BadRequest(new { message = "Thiếu kỳ hoặc định dạng xuất tổng hợp phụ cấp." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext, auditScope, correlationAccessor, AuditActions.AllowanceSummary.Exported,
                token => service.ExportAsync(request, token), cancellationToken, AuditCaptureMode.OperationOnly);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
