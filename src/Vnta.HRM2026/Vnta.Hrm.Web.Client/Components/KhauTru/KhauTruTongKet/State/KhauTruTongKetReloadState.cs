namespace Vnta.Hrm.Web.Client.Components.KhauTru.KhauTruTongHop.State;

/// <summary>
/// Owns the active reload token and request revision for the deduction-summary page.
/// It mirrors the meal-allowance reload boundary so a stale result can never replace newer filters.
/// </summary>
internal sealed class KhauTruTongKetReloadState
{
    public CancellationTokenSource? ActiveRequestTokenSource { get; set; }
    public int RequestedVersion;
}
