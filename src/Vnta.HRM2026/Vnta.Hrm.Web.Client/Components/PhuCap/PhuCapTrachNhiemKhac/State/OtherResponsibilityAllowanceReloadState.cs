namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemKhac;

/// <summary>Owns coalescing state for reload requests.</summary>
internal sealed class OtherResponsibilityAllowanceReloadState : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public int RequestedVersion;
    public int ProcessedVersion;

    public void Dispose() => Gate.Dispose();
}
