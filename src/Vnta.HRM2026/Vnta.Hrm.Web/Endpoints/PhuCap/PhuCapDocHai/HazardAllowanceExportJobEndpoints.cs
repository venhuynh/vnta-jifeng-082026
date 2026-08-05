using Microsoft.AspNetCore.Mvc;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Read-only HTTP boundary for user-owned hazard allowance export jobs.</summary>
internal static class HazardAllowanceExportJobEndpoints
{
    internal static async Task<IResult> GetAsync(
        Guid jobId,
        [FromServices] IHazardAllowanceExportJobService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var job = await service.GetAsync(
            jobId,
            HazardAllowanceEndpointExecution.ResolveActor(httpContext.User),
            cancellationToken);
        return job is null ? Results.NotFound() : Results.Ok(job);
    }

    internal static async Task<IResult> DownloadAsync(
        Guid jobId,
        [FromServices] IHazardAllowanceExportJobService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var file = await service.OpenCompletedFileAsync(
            jobId,
            HazardAllowanceEndpointExecution.ResolveActor(httpContext.User),
            cancellationToken);
        return file is null
            ? Results.NotFound()
            : Results.File(file.Content, file.ContentType, file.FileName, enableRangeProcessing: false);
    }
}
