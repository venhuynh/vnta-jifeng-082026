namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;

/// <summary>Calculates the amount contributed by other-allowance detail lines.</summary>
public static class OtherAllowanceSummaryAmountCalculator
{
    public static decimal CalculateTotal(IEnumerable<OtherAllowanceSummaryLine> allowanceLines)
    {
        ArgumentNullException.ThrowIfNull(allowanceLines);
        return allowanceLines.Sum(line => line.AllowanceAmount);
    }
}

public sealed record OtherAllowanceSummaryLine(decimal AllowanceAmount);
