namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Recalculates hazard allowance snapshots from their business sources.</summary>
public interface IHazardAllowanceRefreshService
{
    Task<RefreshHazardAllowanceResult> RefreshAsync(
        RefreshHazardAllowanceRequest request,
        CancellationToken cancellationToken = default);
}
