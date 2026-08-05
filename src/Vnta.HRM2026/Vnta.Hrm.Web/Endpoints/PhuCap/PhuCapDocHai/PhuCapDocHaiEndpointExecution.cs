using System.Security.Claims;

namespace Vnta.Hrm.Web.Endpoints;

/// <summary>Feature-local HTTP conventions shared by hazard allowance handlers.</summary>
internal static class HazardAllowanceEndpointExecution
{
    internal static HazardAllowanceFilter ResolveFilter(HazardAllowanceFilter? filter)
    {
        if(filter is not null)
        {
            return filter;
        }

        var today = DateTime.Today;
        return new HazardAllowanceFilter(today.Month, today.Year, HazardAllowanceLockState.All, null);
    }

    /// <summary>Never accepts an actor from a JSON command payload.</summary>
    internal static string ResolveActor(ClaimsPrincipal user) =>
        Normalize(user.FindFirst(ClaimTypes.Email)?.Value)
        ?? Normalize(user.FindFirst(ClaimTypes.Name)?.Value)
        ?? Normalize(user.FindFirst(ClaimTypes.NameIdentifier)?.Value)
        ?? "authenticated-user";

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
