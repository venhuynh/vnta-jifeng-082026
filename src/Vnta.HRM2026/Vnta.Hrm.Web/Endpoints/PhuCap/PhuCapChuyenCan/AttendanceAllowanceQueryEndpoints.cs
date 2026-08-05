using Microsoft.AspNetCore.Mvc;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Contracts;
using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;
using Vnta.Hrm.Application.QuanTri.AuditTrail;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapChuyenCan;

/// <summary>Read and export HTTP boundary for attendance allowance.</summary>
internal static class AttendanceAllowanceQueryEndpoints
{
    internal static async Task<IResult> GetRuleAsync(
        [FromServices] IAttendanceAllowanceReadService readService,
        CancellationToken cancellationToken) =>
        Results.Ok(await readService.GetRuleAsync(cancellationToken));

    internal static async Task<IResult> SearchAsync(
        [FromBody] AttendanceAllowanceResultFilter? filter,
        [FromServices] IAttendanceAllowanceReadService readService,
        CancellationToken cancellationToken)
    {
        try
        {
            return Results.Ok(await readService.SearchPageAsync(
                filter ?? new AttendanceAllowanceResultFilter(PayrollAllowanceKind.Attendance, null, null, null),
                cancellationToken));
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    internal static async Task<IResult> ExportAsync(
        [FromBody] AttendanceAllowanceExportRequest? request,
        [FromServices] IAttendanceAllowanceExportService exportService,
        HttpContext httpContext,
        [FromServices] IAuditScope auditScope,
        [FromServices] IAuditCorrelationAccessor correlationAccessor,
        CancellationToken cancellationToken)
    {
        if(request is null)
        {
            return Results.BadRequest(new { message = "Thiáº¿u ká»³ hoáº·c Ä‘á»‹nh dáº¡ng xuáº¥t phá»¥ cáº¥p chuyÃªn cáº§n." });
        }

        try
        {
            var result = await PayrollEndpointExecution.ExecuteAsync(
                httpContext,
                auditScope,
                correlationAccessor,
                AuditActions.AttendanceAllowance.Exported,
                token => exportService.ExportAsync(request, token),
                cancellationToken,
                captureMode: AuditCaptureMode.OperationOnly);
            return Results.Ok(result);
        }
        catch(InvalidOperationException ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}
