namespace Vnta.Hrm.Application.KhauTru.KhauTruPhiCongDoan;

/// <summary>Validates the only mutable value of a union-fee deduction row.</summary>
public static class PayrollUnionFeeDeductionManualValuePolicy
{
    public const decimal MaximumAmount = 9_999_999_999_999_999.99m;

    public static void EnsureValid(decimal deductionAmount)
    {
        if (deductionAmount < 0m || deductionAmount > MaximumAmount)
        {
            throw new InvalidOperationException("Số tiền phí công đoàn phải nằm trong phạm vi cho phép.");
        }

        if (decimal.Round(deductionAmount, 2, MidpointRounding.AwayFromZero) != deductionAmount)
        {
            throw new InvalidOperationException("Số tiền phí công đoàn chỉ được có tối đa 2 chữ số thập phân.");
        }
    }

    public static decimal Normalize(decimal deductionAmount) =>
        decimal.Round(deductionAmount, 2, MidpointRounding.AwayFromZero);
}
