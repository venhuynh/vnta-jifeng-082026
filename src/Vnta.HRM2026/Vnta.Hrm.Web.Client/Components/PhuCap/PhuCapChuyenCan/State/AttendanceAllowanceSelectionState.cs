namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;

/// <summary>
/// Owns grid selection independently from query, loading and mutation state.
/// </summary>
internal sealed class AttendanceAllowanceSelectionState
{
    public IReadOnlyList<object> Items { get; set; } = [];

    public void Clear() => Items = [];
}
