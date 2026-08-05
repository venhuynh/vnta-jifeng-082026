using Vnta.Hrm.Application.Common.Security;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>
/// Registers the allowance-summary HTTP contract beneath the payroll group.
/// Authorization is declared again at this feature boundary so the contract remains protected if it is remapped.
/// </summary>
public static partial class PayrollEndpoints
{
    private static RouteGroupBuilder MapPhuCapTongHopEndpoints(this RouteGroupBuilder payrollGroup)
    {
        var featureGroup = payrollGroup
            .MapGroup("/allowance-summary")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        featureGroup.MapPhuCapDashboardEndpoints();
        featureGroup.MapPost("/summary", PhuCapTongHopQueryEndpoints.GetOverviewAsync);
        featureGroup.MapPost("/search", PhuCapTongHopQueryEndpoints.SearchAsync);
        featureGroup.MapPost("/export", PhuCapTongHopQueryEndpoints.ExportAsync);
        featureGroup.MapPost("/sync-previous-month", PhuCapTongHopCommandEndpoints.SyncFromPreviousMonthAsync);
        featureGroup.MapPost("/refresh", PhuCapTongHopCommandEndpoints.RefreshAsync);
        featureGroup.MapPost("/delete", PhuCapTongHopCommandEndpoints.DeleteAsync);
        featureGroup.MapPost("/manual-adjustment", PhuCapTongHopCommandEndpoints.UpdateManualValuesAsync);
        featureGroup.MapPost("/lock-state", PhuCapTongHopCommandEndpoints.SetLockStateAsync);
        featureGroup.MapPost("/lock-state/batch", PhuCapTongHopCommandEndpoints.SetLockStateBatchAsync);

        return payrollGroup;
    }
}
