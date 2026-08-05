namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;

/// <summary>Normalizes the business definition of one other-allowance line.</summary>
public static class OtherAllowanceDefinitionPolicy
{
    public static OtherAllowanceDefinition Normalize(OtherAllowanceDefinitionInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var allowanceName = input.AllowanceName?.Trim();
        if(string.IsNullOrWhiteSpace(allowanceName) || allowanceName.Length > 256)
            throw new InvalidOperationException("Tên phụ cấp là bắt buộc và không được vượt quá 256 ký tự.");

        var calculatedAmount = OtherAllowanceAmountCalculator.Calculate(new OtherAllowanceAmountInput(
            input.AmountType,
            input.EnteredAllowanceAmount));

        return new OtherAllowanceDefinition(
            allowanceName,
            calculatedAmount.AmountType,
            calculatedAmount.AllowanceAmount,
            NormalizeOptionalText(input.Note));
    }

    internal static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public enum OtherAllowanceAmountType
{
    NonFixed = 0,
    Fixed = 1
}

public sealed record OtherAllowanceDefinitionInput(
    string? AllowanceName,
    OtherAllowanceAmountType AmountType,
    decimal EnteredAllowanceAmount,
    string? Note);

public sealed record OtherAllowanceDefinition(
    string AllowanceName,
    OtherAllowanceAmountType AmountType,
    decimal AllowanceAmount,
    string? Note);
