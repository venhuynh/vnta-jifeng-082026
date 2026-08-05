namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Decides whether a deduction row may be recalculated or manually adjusted.
/// The policy is deliberately independent of EF and only consumes the server row state.
/// </summary>
public static class PayrollInsuranceDeductionLockPolicy
{
    public static PayrollInsuranceDeductionLockDecision Evaluate(
        PayrollInsuranceDeductionLockInput input) =>
        input.DetailIsLocked || input.SummaryIsLocked
            ? PayrollInsuranceDeductionLockDecision.Locked
            : PayrollInsuranceDeductionLockDecision.Allowed;
}

public sealed record PayrollInsuranceDeductionLockInput(
    bool DetailIsLocked,
    bool SummaryIsLocked);

public enum PayrollInsuranceDeductionLockDecision
{
    Allowed = 0,
    Locked = 1
}
