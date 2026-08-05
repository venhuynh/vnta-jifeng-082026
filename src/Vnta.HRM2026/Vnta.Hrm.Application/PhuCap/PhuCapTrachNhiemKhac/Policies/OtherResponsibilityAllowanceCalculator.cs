namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Policies;

public sealed class OtherResponsibilityAllowanceCalculator : IOtherResponsibilityAllowanceCalculator
{
    public OtherResponsibilityAllowanceCalculationResult Calculate(OtherResponsibilityAllowanceCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.StandardResponsibilityAllowanceAmount <= 0m || input.StandardWorkdayCount <= 0m)
        {
            return new OtherResponsibilityAllowanceCalculationResult(0m);
        }

        var missingWorkdays = RoundWorkdays(Math.Max(
            input.StandardWorkdayCount - input.AllowanceCalculationWorkdayCount,
            0m));
        var actualAllowance = missingWorkdays <= 1m
            ? RoundCurrency(input.StandardResponsibilityAllowanceAmount)
            : RoundCurrency(
                input.StandardResponsibilityAllowanceAmount
                / input.StandardWorkdayCount
                * input.AllowanceCalculationWorkdayCount);

        return new OtherResponsibilityAllowanceCalculationResult(actualAllowance);
    }

    private static decimal RoundWorkdays(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal RoundCurrency(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
