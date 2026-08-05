namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapPhepLe;

/// <summary>Mutable UI-only filter state; it deliberately has no service dependency.</summary>
public sealed class PhuCapPhepLeFilterState
{
    public string? SearchText { get; set; }
}

/// <summary>Grid selection state kept apart from page orchestration.</summary>
public sealed class PhuCapPhepLeSelectionState
{
    public IReadOnlyList<object> Items { get; set; } = [];
}
