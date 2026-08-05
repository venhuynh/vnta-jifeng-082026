namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Pure calculation policy for employee-paid BHXH/BHYT/BHTN amounts.
/// Components are rounded independently to whole VND to preserve the existing payroll contract.
/// </summary>
public static class PayrollInsuranceDeductionCalculator
{
    public static PayrollInsuranceDeductionCalculatedValues Calculate(
        PayrollInsuranceDeductionCalculationInput input)
    {
        var participating = input.ParticipationStatus == InsuranceParticipationStatus.Participating;
        var social = participating ? input.SocialInsuranceRate : 0m;
        var health = participating ? input.HealthInsuranceRate : 0m;
        var unemployment = participating ? input.UnemploymentInsuranceRate : 0m;
        var socialAmount = Math.Round(input.InsuranceSalaryBaseAmount * social, 0, MidpointRounding.AwayFromZero);
        var healthAmount = Math.Round(input.InsuranceSalaryBaseAmount * health, 0, MidpointRounding.AwayFromZero);
        var unemploymentAmount = Math.Round(input.InsuranceSalaryBaseAmount * unemployment, 0, MidpointRounding.AwayFromZero);
        return new(
            Math.Round(social + health + unemployment, 4, MidpointRounding.AwayFromZero),
            socialAmount,
            healthAmount,
            unemploymentAmount,
            socialAmount + healthAmount + unemploymentAmount);
    }
}

public sealed record PayrollInsuranceDeductionCalculationInput(
    decimal InsuranceSalaryBaseAmount,
    decimal SocialInsuranceRate,
    decimal HealthInsuranceRate,
    decimal UnemploymentInsuranceRate,
    InsuranceParticipationStatus ParticipationStatus);

public enum InsuranceParticipationStatus
{
    NotParticipating = 0,
    Participating = 1
}

/// <summary>
/// Standard employee-paid rates used by the feature. Persisted/manual rates remain
/// inputs to the calculator so existing adjustment behavior is unchanged.
/// </summary>
public static class PayrollInsuranceDeductionStandardRates
{
    public const decimal SocialInsurance = 0.08m;
    public const decimal HealthInsurance = 0.015m;
    public const decimal UnemploymentInsurance = 0.01m;
    public const decimal Total = 0.105m;
}

public sealed record PayrollInsuranceDeductionCalculatedValues(
    decimal TotalInsuranceRate,
    decimal SocialInsuranceAmount,
    decimal HealthInsuranceAmount,
    decimal UnemploymentInsuranceAmount,
    decimal TotalDeductionAmount);
