using Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;

namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapKhac.Models;

/// <summary>UI preview only; the server recalculates and remains the authority when saving.</summary>
public static class OtherAllowanceAmountPreview
{
    public static decimal Calculate(bool isFixedAmount, decimal enteredAllowanceAmount)
    {
        var amountType = isFixedAmount
            ? OtherAllowanceAmountType.Fixed
            : OtherAllowanceAmountType.NonFixed;
        return OtherAllowanceAmountCalculator.Calculate(new OtherAllowanceAmountInput(
            amountType,
            Math.Max(0m, enteredAllowanceAmount))).AllowanceAmount;
    }
}
