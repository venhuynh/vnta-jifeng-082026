namespace Vnta.Hrm.Web.Client.Navigation;

public sealed record VntaNavMenuNode
{
    public required string Key { get; init; }

    public required string Text { get; init; }

    public string? Route { get; init; }

    public string? IconUrl { get; init; }

    public string? IconCssClass { get; init; }

    public bool IsRoadmapOnly { get; init; }

    public bool IsInProgress { get; init; }

    public IReadOnlyList<VntaNavMenuNode> Children { get; init; } = [];

    public IReadOnlyList<string> AllowedRoles { get; init; } = [];

    public IReadOnlyList<string> AllowedCapabilities { get; init; } = [];

    public IReadOnlyList<string> RouteAliases { get; init; } = [];

    public bool HasChildren => Children.Count > 0;

    public bool IsNavigable => !string.IsNullOrWhiteSpace(Route);
}
