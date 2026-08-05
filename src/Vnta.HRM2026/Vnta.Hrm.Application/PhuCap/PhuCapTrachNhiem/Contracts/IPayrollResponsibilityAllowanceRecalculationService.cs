namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Backend-only command boundary for an atomic responsibility-allowance refresh
/// and calculation. It intentionally stays separate from the UI workflow
/// contract so the additive API does not require a Web.Client change.
/// </summary>
public interface IPayrollResponsibilityAllowanceRecalculationService
{
    Task<RecalculatePayrollResponsibilityAllowanceAbcResult> RecalculateAbcAsync(
        RefreshPayrollResponsibilityAllowanceAbcRequest request,
        CancellationToken cancellationToken = default);
}
