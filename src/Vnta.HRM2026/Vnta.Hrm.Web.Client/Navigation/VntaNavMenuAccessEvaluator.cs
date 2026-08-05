using System.Security.Claims;
namespace Vnta.Hrm.Web.Client.Navigation;

public static class VntaNavMenuAccessEvaluator
{
    public static IReadOnlyList<VntaNavMenuNode> BuildVisibleNodes(ClaimsPrincipal user)
    {
        var roles = user.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var capabilities = InternalAccountCapabilityResolver.ResolveCapabilities(
                user.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(claim => claim.Value),
                user.Claims.Where(claim => claim.Type == InternalAccountClaimTypes.Permission).Select(claim => claim.Value))
            .Select(static capability => capability.Trim().ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (user.Identity?.IsAuthenticated == true)
        {
            roles.Add(VntaNavMenuCatalog.AuthenticatedRole);
        }

        if (roles.Contains(VntaNavMenuCatalog.AdminRole))
        {
            roles.Add(VntaNavMenuCatalog.AuthenticatedRole);
        }

        return FilterNodes(VntaNavMenuCatalog.All, roles, capabilities);
    }

    public static string ResolveCurrentModule(IReadOnlyList<VntaNavMenuNode> nodes, string currentPath)
        => FindCurrentNode(nodes, currentPath)?.Text ?? "Tổng quan";

    public static bool IsExpanded(VntaNavMenuNode node, string currentPath)
        => node.Children.Any(child => MatchesRouteOrDescendant(child, currentPath));

    public static string? ResolveActiveRoute(VntaNavMenuNode node, string currentPath)
    {
        if (node.Route is not null && RoutesEqual(node.Route, currentPath))
        {
            return node.Route;
        }

        return node.RouteAliases.FirstOrDefault(alias => RoutesEqual(alias, currentPath)) ?? node.Route;
    }

    private static IReadOnlyList<VntaNavMenuNode> FilterNodes(
        IReadOnlyList<VntaNavMenuNode> nodes,
        IReadOnlySet<string> roles,
        IReadOnlySet<string> capabilities)
        => nodes
            .Select(node => FilterNode(node, roles, capabilities))
            .Where(node => node is not null)
            .Cast<VntaNavMenuNode>()
            .ToArray();

    private static VntaNavMenuNode? FilterNode(VntaNavMenuNode node, IReadOnlySet<string> roles, IReadOnlySet<string> capabilities)
    {
        if (!CanAccess(node, roles, capabilities))
        {
            return null;
        }

        if (!node.HasChildren)
        {
            return node;
        }

        var visibleChildren = FilterNodes(node.Children, roles, capabilities);
        if (visibleChildren.Count == 0 && !node.IsNavigable)
        {
            return null;
        }

        return node with { Children = visibleChildren };
    }

    private static bool CanAccess(VntaNavMenuNode node, IReadOnlySet<string> roles, IReadOnlySet<string> capabilities)
        => (node.AllowedRoles.Count == 0 || node.AllowedRoles.Any(roles.Contains))
           && (node.AllowedCapabilities.Count == 0 || node.AllowedCapabilities.Any(capabilities.Contains));

    private static VntaNavMenuNode? FindCurrentNode(IReadOnlyList<VntaNavMenuNode> nodes, string currentPath)
    {
        foreach (var node in nodes)
        {
            if (MatchesRoute(node, currentPath))
            {
                return node;
            }

            var match = FindCurrentNode(node.Children, currentPath);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static bool MatchesRouteOrDescendant(VntaNavMenuNode node, string currentPath)
        => MatchesRoute(node, currentPath) || node.Children.Any(child => MatchesRouteOrDescendant(child, currentPath));

    private static bool MatchesRoute(VntaNavMenuNode node, string currentPath)
    {
        if (node.Route is not null && RoutesEqual(node.Route, currentPath))
        {
            return true;
        }

        return node.RouteAliases.Any(alias => RoutesEqual(alias, currentPath));
    }

    private static bool RoutesEqual(string left, string right)
        => string.Equals(NormalizeRoute(left), NormalizeRoute(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRoute(string route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "/";
        }

        var normalized = route.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        if (normalized.Length > 1 && normalized.EndsWith('/'))
        {
            normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }
}
