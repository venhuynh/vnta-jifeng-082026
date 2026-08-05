namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;

/// <summary>Validates the amount that a payroll operator manually enters for "Khấu trừ khác".</summary>
public static class PayrollDeductionSummaryManualOtherDeductionPolicy
{
    public static void Validate(PayrollDeductionSummaryManualOtherDeductionValidationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if(input.RequestedOtherDeductionAmount < 0m)
        {
            throw new InvalidOperationException("Khoản khấu trừ khác không được nhỏ hơn 0.");
        }

        if(decimal.Round(input.RequestedOtherDeductionAmount, 2, MidpointRounding.AwayFromZero)
            != input.RequestedOtherDeductionAmount)
        {
            throw new InvalidOperationException("Khoản khấu trừ khác chỉ được nhập tối đa 2 chữ số thập phân.");
        }
    }
}

public sealed record PayrollDeductionSummaryManualOtherDeductionValidationInput(
    decimal RequestedOtherDeductionAmount);
