using Vnta.Hrm.Application.Common.Security;

namespace Vnta.Hrm.Web.Endpoints.PhuCap.PhuCapTrachNhiemKhac;

/// <summary>Registers the compatible HTTP contract for other responsibility allowance.</summary>
public static class PhuCapTrachNhiemKhacEndpointMappings
{
    public static RouteGroupBuilder MapPhuCapTrachNhiemKhacEndpoints(this RouteGroupBuilder payrollGroup)
    {
        // Retain the legacy paths while making the feature's authorization explicit at its boundary.
        var featureGroup = payrollGroup
            .MapGroup("/other-responsibility-allowance")
            .RequireAuthorization(InternalAccountPolicies.PayrollAdministration);

        featureGroup.MapOtherResponsibilityAllowanceQueryEndpoints();
        featureGroup.MapOtherResponsibilityAllowanceCommandEndpoints();
        return featureGroup;
    }
}
