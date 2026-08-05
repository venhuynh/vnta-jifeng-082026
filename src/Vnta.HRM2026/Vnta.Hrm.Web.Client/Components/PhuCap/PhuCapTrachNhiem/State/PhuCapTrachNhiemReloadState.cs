namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Owns request versioning and the cancellable request currently loading the
/// responsibility-allowance snapshot.
/// </summary>
internal sealed class PhuCapTrachNhiemReloadState
{
    public CancellationTokenSource? ActiveRequestTokenSource { get; set; }

    public int RequestedVersion;

    public int ProcessedVersion;
}
