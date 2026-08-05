using Vnta.Hrm.Application.Common.Security;

namespace Vnta.Hrm.Web.Endpoints;

public static partial class PayrollEndpoints
{
    private static RouteGroupBuilder MapPhuCapThamNienEndpoints(this RouteGroupBuilder payrollGroup)
    {
        var featureGroup = payrollGroup
            .MapGroup("/seniority-allowance")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        featureGroup.MapPost("/prepare-period", PrepareSeniorityAllowancePeriodAsync);
        featureGroup.MapGet(string.Empty, SearchSeniorityAllowancesAsync);
        featureGroup.MapGet("/search-page", SearchSeniorityAllowancePageAsync);
        featureGroup.MapGet("/range-summaries", GetSeniorityAllowanceRangeSummariesAsync);
        featureGroup.MapPost("/refresh", RefreshSeniorityAllowancesAsync);
        featureGroup.MapPost("/manual-values", UpdateSeniorityAllowanceManualValuesAsync);
        featureGroup.MapPost("/lock-state", SetSeniorityAllowanceLockStateAsync);
        featureGroup.MapPost("/lock-state/batch", SetSeniorityAllowanceBatchLockStateAsync);
        return payrollGroup;
    }
}
