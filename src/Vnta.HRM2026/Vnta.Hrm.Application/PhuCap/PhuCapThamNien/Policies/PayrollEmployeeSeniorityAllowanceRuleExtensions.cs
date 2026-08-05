namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>Maps seniority allowance rules to their existing persisted keys.</summary>
public static class PayrollEmployeeSeniorityAllowanceRuleExtensions
{
    public static string ToStorageKey(this PayrollEmployeeSeniorityAllowanceRule rule) => rule switch
    {
        PayrollEmployeeSeniorityAllowanceRule.TemporaryPosition => "temporary-position",
        PayrollEmployeeSeniorityAllowanceRule.SalaryWorkDaysAtOrBelowMinimum => "blocked-salary-work",
        PayrollEmployeeSeniorityAllowanceRule.ThirteenYearsOrMore => "13-plus",
        PayrollEmployeeSeniorityAllowanceRule.TenToUnderThirteenYears => "10-13",
        PayrollEmployeeSeniorityAllowanceRule.SixToUnderTenYears => "6-10",
        PayrollEmployeeSeniorityAllowanceRule.ThreeToUnderSixYears => "3-6",
        PayrollEmployeeSeniorityAllowanceRule.OneToUnderThreeYears => "1-3",
        PayrollEmployeeSeniorityAllowanceRule.NoAllowance => "none",
        _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unknown seniority allowance rule.")
    };

    public static bool TryFromStorageKey(string? storageKey, out PayrollEmployeeSeniorityAllowanceRule rule)
    {
        rule = storageKey switch
        {
            "temporary-position" => PayrollEmployeeSeniorityAllowanceRule.TemporaryPosition,
            "blocked-salary-work" => PayrollEmployeeSeniorityAllowanceRule.SalaryWorkDaysAtOrBelowMinimum,
            "13-plus" => PayrollEmployeeSeniorityAllowanceRule.ThirteenYearsOrMore,
            "10-13" => PayrollEmployeeSeniorityAllowanceRule.TenToUnderThirteenYears,
            "6-10" => PayrollEmployeeSeniorityAllowanceRule.SixToUnderTenYears,
            "3-6" => PayrollEmployeeSeniorityAllowanceRule.ThreeToUnderSixYears,
            "1-3" => PayrollEmployeeSeniorityAllowanceRule.OneToUnderThreeYears,
            "none" => PayrollEmployeeSeniorityAllowanceRule.NoAllowance,
            _ => default
        };

        return storageKey is "temporary-position" or "blocked-salary-work" or "13-plus" or "10-13" or "6-10" or "3-6" or "1-3" or "none";
    }

    public static string ToDisplayText(this PayrollEmployeeSeniorityAllowanceRule rule) => rule switch
    {
        PayrollEmployeeSeniorityAllowanceRule.TemporaryPosition => "Chức vụ Thời vụ",
        PayrollEmployeeSeniorityAllowanceRule.SalaryWorkDaysAtOrBelowMinimum => "Công tính lương <= 5",
        PayrollEmployeeSeniorityAllowanceRule.ThirteenYearsOrMore => "Từ 13 năm trở lên",
        PayrollEmployeeSeniorityAllowanceRule.TenToUnderThirteenYears => "Từ 10 đến dưới 13 năm",
        PayrollEmployeeSeniorityAllowanceRule.SixToUnderTenYears => "Từ 6 đến dưới 10 năm",
        PayrollEmployeeSeniorityAllowanceRule.ThreeToUnderSixYears => "Từ 3 đến dưới 6 năm",
        PayrollEmployeeSeniorityAllowanceRule.OneToUnderThreeYears => "Từ 1 đến dưới 3 năm",
        PayrollEmployeeSeniorityAllowanceRule.NoAllowance => "Chưa đủ 1 năm",
        _ => string.Empty
    };
}
