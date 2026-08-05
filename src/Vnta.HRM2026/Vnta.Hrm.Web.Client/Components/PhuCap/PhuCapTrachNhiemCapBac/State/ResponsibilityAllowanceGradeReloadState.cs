namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapTrachNhiemCapBac;

/// <summary>Owns request versioning and cancellation for the grades snapshot.</summary>
internal sealed class ResponsibilityAllowanceGradeReloadState
{
    public CancellationTokenSource? ActiveRequestTokenSource { get; set; }

    public int RequestedVersion;

    public int ProcessedVersion;
}
