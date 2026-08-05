namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapChuyenCan.State;

/// <summary>Owns request revisions and the active cancellation token for reloads.</summary>
internal sealed class AttendanceAllowanceReloadLifecycleState
{
    public CancellationTokenSource? ActiveRequestTokenSource { get; set; }
    public int RequestedVersion;
    public int ProcessedVersion;
}
