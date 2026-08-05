namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>Values permitted for a server-authoritative manual hazard allowance adjustment.</summary>
public sealed record HazardAllowanceManualAdjustmentInput(
    decimal QualifiedWorkdayCount,
    decimal LateEarlyDeductionDays,
    decimal HazardAllowancePerDay,
    decimal HazardAllowanceAmount,
    bool IsEligibleDepartment,
    string? ExclusionReason);

/// <summary>Normalized snapshot values ready to persist after a manual adjustment.</summary>
public sealed record HazardAllowanceManualAdjustmentResult(
    decimal QualifiedWorkdayCount,
    decimal LateEarlyDeductionDays,
    decimal PayableWorkdayCount,
    decimal HazardAllowancePerDay,
    decimal HazardAllowanceAmount,
    bool IsEligibleDepartment,
    string? ExclusionReason);

public interface IHazardAllowanceManualAdjustmentPolicy
{
    HazardAllowanceManualAdjustmentResult ValidateAndNormalize(HazardAllowanceManualAdjustmentInput input);
}

/// <summary>Validates and normalizes only the manual-edit business rules.</summary>
public sealed class HazardAllowanceManualAdjustmentPolicy : IHazardAllowanceManualAdjustmentPolicy
{
    public HazardAllowanceManualAdjustmentResult ValidateAndNormalize(HazardAllowanceManualAdjustmentInput input)
    {
        if (input.QualifiedWorkdayCount < 0m
            || input.LateEarlyDeductionDays < 0m
            || input.HazardAllowancePerDay < 0m
            || input.HazardAllowanceAmount < 0m)
        {
            throw new InvalidOperationException("Các giá trị số của phụ cấp độc hại không được nhỏ hơn 0.");
        }

        if (input.LateEarlyDeductionDays > input.QualifiedWorkdayCount)
        {
            throw new InvalidOperationException("Công khấu trừ và công tính phụ cấp không được lớn hơn công hợp lệ.");
        }

        var exclusionReason = NormalizeExclusionReason(input);
        var qualifiedWorkdayCount = decimal.Round(input.QualifiedWorkdayCount, 2, MidpointRounding.AwayFromZero);
        var lateEarlyDeductionDays = decimal.Round(input.LateEarlyDeductionDays, 4, MidpointRounding.AwayFromZero);
        return new HazardAllowanceManualAdjustmentResult(
            qualifiedWorkdayCount,
            lateEarlyDeductionDays,
            decimal.Round(
                Math.Max(0m, qualifiedWorkdayCount - lateEarlyDeductionDays),
                4,
                MidpointRounding.AwayFromZero),
            RoundVnd(input.HazardAllowancePerDay),
            RoundVnd(input.HazardAllowanceAmount),
            input.IsEligibleDepartment,
            exclusionReason);
    }

    private static string? NormalizeExclusionReason(HazardAllowanceManualAdjustmentInput input)
    {
        if (!input.IsEligibleDepartment)
        {
            if (string.IsNullOrWhiteSpace(input.ExclusionReason))
            {
                throw new InvalidOperationException("Hãy nhập lý do loại trừ khi nhân viên không đủ điều kiện hưởng phụ cấp độc hại.");
            }

            if (input.ExclusionReason.Trim().Length > 1000)
            {
                throw new InvalidOperationException("Lý do loại trừ không được vượt quá 1.000 ký tự.");
            }

            if (input.HazardAllowanceAmount != 0m)
            {
                throw new InvalidOperationException("Phụ cấp độc hại phải bằng 0 khi nhân viên không đủ điều kiện hưởng.");
            }

            return input.ExclusionReason.Trim();
        }

        if (!string.IsNullOrWhiteSpace(input.ExclusionReason) && input.ExclusionReason.Trim().Length > 1000)
        {
            throw new InvalidOperationException("Lý do loại trừ không được vượt quá 1.000 ký tự.");
        }

        return null;
    }

    private static decimal RoundVnd(decimal value) =>
        decimal.Round(value, 0, MidpointRounding.AwayFromZero);
}
