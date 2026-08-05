namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTongHop;

/// <summary>Owns reload revisions and the active request cancellation source.</summary>
internal sealed class PhuCapTongHopReloadState
{
    public CancellationTokenSource? ActiveRequestTokenSource { get; set; }
    public int RequestedVersion;
    public int ProcessedVersion;
}
