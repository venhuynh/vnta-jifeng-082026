namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapCom;

/// <summary>
/// Owns the reload revision and the active request token. Keeping these values
/// together makes stale-result protection explicit and prevents sections from
/// participating in request lifecycle management.
/// </summary>
internal sealed class PhuCapComReloadState
{
    public CancellationTokenSource? ActiveRequestTokenSource { get; set; }
    public int RequestedVersion;
    public int ProcessedVersion;
}
