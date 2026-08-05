namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;

/// <summary>Calculates the amount that is stored for an other-allowance amount type.</summary>
public static class OtherAllowanceAmountCalculator
{
    public static OtherAllowanceCalculatedAmount Calculate(OtherAllowanceAmountInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if(!Enum.IsDefined(input.AmountType))
            throw new InvalidOperationException("Loại số tiền phụ cấp không hợp lệ.");
        if(input.EnteredAllowanceAmount < 0m)
            throw new InvalidOperationException("Số tiền phụ cấp không được nhỏ hơn 0.");

        var allowanceAmount = input.AmountType == OtherAllowanceAmountType.Fixed
            ? decimal.Round(input.EnteredAllowanceAmount, 0, MidpointRounding.AwayFromZero)
            : 0m;
        return new OtherAllowanceCalculatedAmount(input.AmountType, allowanceAmount);
    }
}

public sealed record OtherAllowanceAmountInput(
    OtherAllowanceAmountType AmountType,
    decimal EnteredAllowanceAmount);

public sealed record OtherAllowanceCalculatedAmount(
    OtherAllowanceAmountType AmountType,
    decimal AllowanceAmount);
