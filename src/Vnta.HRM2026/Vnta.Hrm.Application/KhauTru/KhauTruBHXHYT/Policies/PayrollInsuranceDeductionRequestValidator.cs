namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Validates request values that do not require persistence access.
/// The database enforces the same rate invariant, so callers receive a domain
/// validation error instead of a provider-specific constraint failure.
/// </summary>
public static class PayrollInsuranceDeductionRequestValidator
{
    public static string? Validate(
        PayrollInsuranceDeductionValidationInput input,
        string insuranceSalaryBaseNegativeMessage)
    {
        if (input.InsuranceSalaryBaseAmount < 0)
        {
            return insuranceSalaryBaseNegativeMessage;
        }

        if (input.SocialInsuranceRate is < 0 or > 1
            || input.HealthInsuranceRate is < 0 or > 1
            || input.UnemploymentInsuranceRate is < 0 or > 1)
        {
            return "Tỷ lệ BHXH, BHYT và BHTN phải nằm trong khoảng 0% đến 100%.";
        }

        if (input.SocialInsuranceRate + input.HealthInsuranceRate + input.UnemploymentInsuranceRate > 1)
        {
            return "Tổng tỷ lệ BHXH, BHYT và BHTN không được vượt quá 100%.";
        }

        return input.ParticipationChangeType is < 0 or > 3
            ? "Loại biến động tham gia không hợp lệ."
            : null;
    }

    public static void EnsureValid(
        PayrollInsuranceDeductionValidationInput input,
        string insuranceSalaryBaseNegativeMessage)
    {
        var message = Validate(
            input,
            insuranceSalaryBaseNegativeMessage);

        if (!string.IsNullOrWhiteSpace(message))
        {
            throw new InvalidOperationException(message);
        }
    }
}

public sealed record PayrollInsuranceDeductionValidationInput(
    decimal InsuranceSalaryBaseAmount,
    decimal SocialInsuranceRate,
    decimal HealthInsuranceRate,
    decimal UnemploymentInsuranceRate,
    short ParticipationChangeType);
