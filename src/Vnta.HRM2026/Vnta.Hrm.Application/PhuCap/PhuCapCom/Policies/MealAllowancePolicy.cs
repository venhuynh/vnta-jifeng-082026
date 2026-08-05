namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Policies;

/// <summary>Quy tắc thuần xác định ngày đủ điều kiện và tiền phụ cấp cơm.</summary>
public static class MealAllowancePolicy
{
    public const int DefaultMealAllowancePerQualifiedDay = 18000;
    public const int MinimumQualifyingOvertimeMinutes15 = 120;
    public const int MaximumQualifyingOvertimeMinutes15 = 150;
    public const string QualifiedMealRuleCode = "qualified-meal";
    public const string QualifiedMealRuleVersion = "2026-07-meal-v1";
    public const string ManualAdjustmentRuleCode = "manual-adjustment";
    public const string ManualAdjustmentRuleVersion = "2026-07-meal-manual-v1";

    public static MealAllowanceWorkdayEligibility EvaluateWorkday(MealAllowanceWorkday workday)
    {
        ArgumentNullException.ThrowIfNull(workday);

        if(!IsRegularWorkday(workday.WorkdayType))
        {
            return MealAllowanceWorkdayEligibility.NotRegularWorkday;
        }

        if(!IsProductionShift(workday.Shift))
        {
            return MealAllowanceWorkdayEligibility.NotProductionShift;
        }

        return workday.OvertimeMinutesAtRate15 is < MinimumQualifyingOvertimeMinutes15 or > MaximumQualifyingOvertimeMinutes15
            ? MealAllowanceWorkdayEligibility.OvertimeMinutesOutsideQualifyingRange
            : MealAllowanceWorkdayEligibility.Qualifies;
    }

    public static MealAllowanceCalculationResult Calculate(MealAllowanceCalculationInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(input.Workdays);

        var qualifiedMealDays = input.Workdays.Count(workday =>
            EvaluateWorkday(workday) == MealAllowanceWorkdayEligibility.Qualifies);
        var unitPrice = Math.Max(0m, input.MealAllowancePerQualifiedDay);

        return new MealAllowanceCalculationResult(
            qualifiedMealDays,
            qualifiedMealDays,
            unitPrice,
            CalculateAllowanceAmount(new MealAllowanceAmountInput(qualifiedMealDays, unitPrice)));
    }

    public static decimal CalculateAllowanceAmount(MealAllowanceAmountInput input) =>
        decimal.Round(
            Math.Max(0, input.AllowanceDayCount) * Math.Max(0m, input.MealAllowancePerQualifiedDay),
            2,
            MidpointRounding.AwayFromZero);

    private static bool IsRegularWorkday(string? workdayType)
    {
        var normalizedValue = NormalizeText(workdayType);
        return normalizedValue is "regular" or "workday" or "working-day" or "normal"
            || normalizedValue.Contains("ngay thuong", StringComparison.Ordinal);
    }

    private static bool IsProductionShift(MealAllowanceShift shift)
    {
        ArgumentNullException.ThrowIfNull(shift);

        var normalizedCode = NormalizeCode(shift.ShortName)
            ?? NormalizeCode(shift.Code)
            ?? string.Empty;

        if(normalizedCode.StartsWith("SX", StringComparison.Ordinal))
        {
            return true;
        }

        var normalizedText = NormalizeText(shift.ShortName, shift.Name);
        return normalizedText.Contains("san xuat", StringComparison.Ordinal)
            || normalizedText.Contains("production", StringComparison.Ordinal);
    }

    private static string? NormalizeCode(string? value)
    {
        var normalized = NormalizeText(value);
        return string.IsNullOrWhiteSpace(normalized)
            ? null
            : normalized.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }

    private static string NormalizeText(params string?[] values)
    {
        var combined = string.Join(
            " ",
            values.Where(static value => !string.IsNullOrWhiteSpace(value)).Select(static value => value!.Trim()));

        if(string.IsNullOrWhiteSpace(combined))
        {
            return string.Empty;
        }

        var normalized = combined.Normalize(System.Text.NormalizationForm.FormD);
        var buffer = new char[normalized.Length];
        var index = 0;

        foreach(var character in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character);
            if(category == System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            buffer[index++] = character switch
            {
                'đ' => 'd',
                'Đ' => 'D',
                _ => character
            };
        }

        return new string(buffer, 0, index)
            .Normalize(System.Text.NormalizationForm.FormC)
            .ToLowerInvariant();
    }
}

/// <summary>Dữ liệu ca dùng để xác định một ngày công có thuộc ca sản xuất.</summary>
public sealed record MealAllowanceShift(string? Code, string? Name, string? ShortName);

/// <summary>Dữ kiện công của một dòng ngày, độc lập với EF và HTTP.</summary>
public sealed record MealAllowanceWorkday(
    string? WorkdayType,
    MealAllowanceShift Shift,
    int OvertimeMinutesAtRate15);

/// <summary>Đầu vào phép tính phụ cấp cơm của một nhân viên trong một kỳ.</summary>
public sealed record MealAllowanceCalculationInput(
    IReadOnlyCollection<MealAllowanceWorkday> Workdays,
    decimal MealAllowancePerQualifiedDay);

/// <summary>Đầu vào phép nhân tiền phụ cấp với tên đơn vị nghiệp vụ rõ ràng.</summary>
public sealed record MealAllowanceAmountInput(int AllowanceDayCount, decimal MealAllowancePerQualifiedDay);

/// <summary>Kết quả tính, trong đó hai số ngày được giữ riêng để tương thích snapshot hiện hữu.</summary>
public sealed record MealAllowanceCalculationResult(
    int QualifiedMealDays,
    int Overtime1900Days,
    decimal MealAllowancePerQualifiedDay,
    decimal MealAllowanceAmount);

public enum MealAllowanceWorkdayEligibility
{
    Qualifies = 0,
    NotRegularWorkday = 1,
    NotProductionShift = 2,
    OvertimeMinutesOutsideQualifyingRange = 3
}
