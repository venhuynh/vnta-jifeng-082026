using System.Security.Claims;

namespace Vnta.Hrm.Application.Common.Security;

public static class InternalAccountCapabilityResolver
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> CapabilitiesByRole =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [InternalAccountRoles.Admin] =
            [
                InternalAccountCapabilities.EmployeeAccountsOpen
            ],
            [InternalAccountRoles.SystemAdmin] =
            [
                InternalAccountCapabilities.EmployeeAccountsOpen,
                InternalAccountCapabilities.EmployeeAccountsApprove,
                InternalAccountCapabilities.AuditRead
            ],
            [InternalAccountRoles.HrAdmin] =
            [
                InternalAccountCapabilities.EmployeeAccountsOpen,
                InternalAccountCapabilities.EmployeeAccountsApprove
            ]
        };

    public static bool HasCapability(ClaimsPrincipal user, string capability)
        => ResolveCapabilities(
                user.FindAll(ClaimTypes.Role).Select(static claim => claim.Value),
                user.FindAll(InternalAccountClaimTypes.Permission).Select(static claim => claim.Value))
            .Contains(capability, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> ResolveCapabilities(
        IEnumerable<string> roles,
        IEnumerable<string> explicitCapabilities)
    {
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach(var capability in explicitCapabilities
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim()))
        {
            resolved.Add(capability);
        }

        foreach(var role in roles
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim()))
        {
            if(!CapabilitiesByRole.TryGetValue(role, out var roleCapabilities))
            {
                continue;
            }

            foreach(var capability in roleCapabilities)
            {
                resolved.Add(capability);
            }
        }

        return resolved
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
