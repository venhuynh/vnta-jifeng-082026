namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Maps the unchanged hazard allowance HTTP contract under the payroll group.</summary>
public static partial class PayrollEndpoints
{
    private static RouteGroupBuilder MapPhuCapDocHaiEndpoints(this RouteGroupBuilder payrollGroup)
    {
        // Keep the public paths verbatim while making authorization explicit at
        // this feature boundary as well as on the parent payroll group.
        var hazardAllowanceGroup = payrollGroup
            .MapGroup("/hazard-allowance")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        hazardAllowanceGroup.MapPost("/search", HazardAllowanceQueryEndpoints.SearchAsync);
        hazardAllowanceGroup.MapPost("/search-page", HazardAllowanceQueryEndpoints.SearchPageAsync);
        hazardAllowanceGroup.MapPost("/summary", HazardAllowanceQueryEndpoints.SummaryAsync);
        hazardAllowanceGroup.MapPost("/export", HazardAllowanceQueryEndpoints.ExportAsync);
        hazardAllowanceGroup.MapGet("/export-jobs/{jobId:guid}", HazardAllowanceExportJobEndpoints.GetAsync);
        hazardAllowanceGroup.MapGet("/export-jobs/{jobId:guid}/download", HazardAllowanceExportJobEndpoints.DownloadAsync);
        hazardAllowanceGroup.MapPost("/export-jobs", HazardAllowanceCommandEndpoints.QueueExportAsync);
        hazardAllowanceGroup.MapPost("/refresh", HazardAllowanceCommandEndpoints.RefreshAsync);
        hazardAllowanceGroup.MapPost("/manual-values", HazardAllowanceCommandEndpoints.UpdateManualValuesAsync);
        hazardAllowanceGroup.MapPost("/entitlement/batch", HazardAllowanceCommandEndpoints.SetEntitlementBatchAsync);
        hazardAllowanceGroup.MapPost("/lock-state", HazardAllowanceCommandEndpoints.SetLockStateAsync);
        hazardAllowanceGroup.MapPost("/lock-state/batch", HazardAllowanceCommandEndpoints.SetLockStateBatchAsync);
        return payrollGroup;
    }
}
