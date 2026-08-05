using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop;
using Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Contracts;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Web.Endpoints;

namespace Vnta.Hrm.Web.Endpoints.KhauTru.KhauTruTongKet;

/// <summary>Query/export routes for deduction summary.</summary>
public static class KhauTruTongKetQueryEndpoints
{
    public static RouteGroupBuilder MapKhauTruTongKetQueryEndpoints(this RouteGroupBuilder payrollGroup)
    {
        payrollGroup.MapPost("/deduction-summary/search", SearchAsync);
        payrollGroup.MapPost("/deduction-summary/export", ExportAsync);
        return payrollGroup;
    }

    private static async Task<IResult> SearchAsync(
        [FromBody] PayrollDeductionSummaryFilter? filter,
        [FromServices] IPayrollDeductionSummaryReadService service,
        [FromServices] IPayrollDeductionSummaryRequestValidator requestValidator,
        CancellationToken cancellationToken)
    {
        try
        {
            var effectiveFilter = filter ?? new PayrollDeductionSummaryFilter(null, null, null);
            var validation = requestValidator.Validate(effectiveFilter);
            if(!validation.IsValid)
                return Results.BadRequest(new { message = validation.ErrorMessage });
            return Results.Ok(await service.SearchAsync(effectiveFilter, cancellationToken));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }

    private static async Task<IResult> ExportAsync(
        [FromBody] PayrollDeductionSummaryExportRequest? request,
        [FromServices] IPayrollDeductionSummaryExportService service,
        [FromServices] IPayrollDeductionSummaryRequestValidator requestValidator,
        HttpContext context,
        [FromServices] IAuditScope audit,
        [FromServices] IAuditCorrelationAccessor correlation,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return Results.BadRequest(new { message = "Thiếu kỳ hoặc định dạng xuất tổng kết khấu trừ." });
        var validation = requestValidator.Validate(request);
        if(!validation.IsValid)
            return Results.BadRequest(new { message = validation.ErrorMessage });

        try
        {
            return Results.Ok(await PayrollEndpointExecution.ExecuteAsync(
                context, audit, correlation, AuditActions.DeductionSummary.Exported,
                token => service.ExportPeriodAsync(request.PayrollMonth, request.PayrollYear, request.Format, token),
                cancellationToken, AuditCaptureMode.OperationOnly));
        }
        catch (KeyNotFoundException ex) { return Results.NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Results.BadRequest(new { message = ex.Message }); }
    }
}
