namespace Vnta.Hrm.Application.PhuCap.PhuCapKhac.Policies;

/// <summary>Validates and normalizes the read-model search input.</summary>
public static class OtherAllowanceSearchPolicy
{
    public static void ValidatePayrollPeriod(int payrollYear, int payrollMonth)
    {
        if(payrollYear is < 1 or > 9999 || payrollMonth is < 1 or > 12)
            throw new InvalidOperationException("Kỳ lương không hợp lệ.");
    }

    public static string? NormalizeSearchText(string? value) =>
        OtherAllowanceDefinitionPolicy.NormalizeOptionalText(value);
}
