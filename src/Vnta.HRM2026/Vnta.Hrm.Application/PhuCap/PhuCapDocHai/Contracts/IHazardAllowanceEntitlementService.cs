namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Changes the user-controlled entitlement state for selected, unlocked hazard snapshots.</summary>
public interface IHazardAllowanceEntitlementService
{
    Task<SetHazardAllowanceEntitlementBatchResult> SetEntitlementBatchAsync(
        SetHazardAllowanceEntitlementBatchRequest request,
        CancellationToken cancellationToken = default);
}
