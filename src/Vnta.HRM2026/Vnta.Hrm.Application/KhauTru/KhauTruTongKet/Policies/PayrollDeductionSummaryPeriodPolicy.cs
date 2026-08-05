namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop.Policies;

/// <summary>
/// Pure period rules shared by deduction-summary commands and queries. It has no persistence,
/// HTTP or UI dependency; adapters decide how to surface the resulting validation exception.
/// </summary>
public static class PayrollDeductionSummaryPeriodPolicy
{
    public const int MinimumSupportedMonth = 6;
    public const int MinimumSupportedYear = 2026;
    public const int MaximumSupportedYear = 2100;

    public static void ValidateRequired(int year, int month)
    {
        switch(EvaluateRequired(year, month))
        {
            case PayrollDeductionSummaryPeriodValidationStatus.YearOutOfRange:
                throw new InvalidOperationException($"Nam du lieu phai nam trong khoang {MinimumSupportedYear} den {MaximumSupportedYear}.");
            case PayrollDeductionSummaryPeriodValidationStatus.MonthOutOfRange:
                throw new InvalidOperationException("Thang du lieu phai nam trong khoang 1 den 12.");
            case PayrollDeductionSummaryPeriodValidationStatus.BeforeFirstSupportedMonth:
                throw new InvalidOperationException($"Du lieu tong ket khau tru bat dau tu {MinimumSupportedMonth:00}/{MinimumSupportedYear}.");
        }
    }

    public static PayrollDeductionSummaryPeriodValidationStatus EvaluateRequired(int year, int month)
    {
        if(year < MinimumSupportedYear || year > MaximumSupportedYear)
            return PayrollDeductionSummaryPeriodValidationStatus.YearOutOfRange;
        if(month is < 1 or > 12)
            return PayrollDeductionSummaryPeriodValidationStatus.MonthOutOfRange;
        return year == MinimumSupportedYear && month < MinimumSupportedMonth
            ? PayrollDeductionSummaryPeriodValidationStatus.BeforeFirstSupportedMonth
            : PayrollDeductionSummaryPeriodValidationStatus.Supported;
    }

    public static void ValidateSearch(int? year, int? month)
    {
        if(year.HasValue && (year.Value < MinimumSupportedYear || year.Value > MaximumSupportedYear))
            throw new InvalidOperationException($"Nam du lieu phai nam trong khoang {MinimumSupportedYear} den {MaximumSupportedYear}.");
        if(month.HasValue && (month.Value < 1 || month.Value > 12))
            throw new InvalidOperationException("Thang du lieu phai nam trong khoang 1 den 12.");
        if(year == MinimumSupportedYear && month.HasValue && month.Value < MinimumSupportedMonth)
            throw new InvalidOperationException($"Du lieu tong ket khau tru bat dau tu {MinimumSupportedMonth:00}/{MinimumSupportedYear}.");
    }

    public static (short Year, short Month) Previous(short year, short month) =>
        month == 1 ? ((short)(year - 1), (short)12) : (year, (short)(month - 1));
}

public enum PayrollDeductionSummaryPeriodValidationStatus
{
    Supported,
    YearOutOfRange,
    MonthOutOfRange,
    BeforeFirstSupportedMonth
}
