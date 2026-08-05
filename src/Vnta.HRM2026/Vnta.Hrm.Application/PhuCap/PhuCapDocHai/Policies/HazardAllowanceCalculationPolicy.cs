using System.Globalization;
using System.Text;

namespace Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

/// <summary>
/// Calculates a hazard-allowance snapshot from already collected monthly metrics.
/// This policy intentionally has no dependency on persistence, HTTP, or UI state.
/// </summary>
public sealed class HazardAllowanceCalculationPolicy : IHazardAllowanceCalculationPolicy
{
    public const decimal AllowancePerPayableWorkday = 7_700m;

    public HazardAllowanceCalculationResult Calculate(HazardAllowanceCalculationInput input)
    {
        // A deduction cannot reduce payable workdays below zero. This is a
        // domain invariant and must be enforced before the snapshot reaches
        // persistence (which has the same non-negative constraint).
        var payableWorkdays = decimal.Round(
            Math.Max(0m, input.QualifiedWorkdayCount - input.LateEarlyDeductionDays),
            4,
            MidpointRounding.AwayFromZero);
        var mandatoryExclusionReason = GetMandatoryExclusionReason(input.DepartmentPath, input.PositionName);
        var isEligibleDepartment = mandatoryExclusionReason is null && IsEligibleDepartment(input.DepartmentPath);
        // Mandatory exclusions are evaluated before the user-controlled state. They cannot be
        // bypassed by choosing "Hưởng PC" in the toolbar.
        var isEligibleForAllowance = mandatoryExclusionReason is null
            && (input.IsEligibleForAllowance ?? isEligibleDepartment);
        // CTL has already been rounded to four decimal places. Multiplication by the fixed
        // rate intentionally performs no additional rounding.
        var allowanceAmount = payableWorkdays * AllowancePerPayableWorkday;

        return new HazardAllowanceCalculationResult(
            input.QualifiedWorkdayCount,
            input.LateEarlyDeductionDays,
            payableWorkdays,
            HazardAllowancePerDay: isEligibleForAllowance ? AllowancePerPayableWorkday : 0m,
            HazardAllowanceAmount: isEligibleForAllowance ? allowanceAmount : 0m,
            isEligibleDepartment,
            isEligibleForAllowance
                ? null
                : mandatoryExclusionReason ?? "Ngoại lệ do người dùng chọn.",
            isEligibleForAllowance);
    }

    private static string? GetMandatoryExclusionReason(string? departmentPath, string? positionName)
    {
        var normalizedPath = NormalizeForCompare(departmentPath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return BuildExclusionReason(departmentPath);
        }

        var pathSegments = normalizedPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (pathSegments.Length > 0
            && (string.Equals(pathSegments[0], "van phong", StringComparison.Ordinal)
                || string.Equals(pathSegments[0], "khoi van phong", StringComparison.Ordinal)))
        {
            return "Bộ phận thuộc Khối Văn phòng không hưởng phụ cấp độc hại.";
        }

        if(pathSegments.Length >= 3
            && (string.Equals(pathSegments[0], "san xuat", StringComparison.Ordinal)
                || string.Equals(pathSegments[0], "khoi san xuat", StringComparison.Ordinal))
            && (string.Equals(pathSegments[1], "phong ke toan", StringComparison.Ordinal)
                || string.Equals(pathSegments[1], "p. ke toan", StringComparison.Ordinal))
            && string.Equals(pathSegments[2], "kho vat tu", StringComparison.Ordinal))
        {
            return "Bộ phận Sản xuất / Phòng kế toán / Kho vật tư không hưởng phụ cấp độc hại.";
        }

        if (NormalizeForCompare(positionName).Contains("thoi vu", StringComparison.Ordinal))
        {
            return "Chức vụ có chứa Thời vụ không hưởng phụ cấp độc hại.";
        }

        return null;
    }

    private static bool IsEligibleDepartment(string? departmentPath) =>
        !string.IsNullOrWhiteSpace(NormalizeForCompare(departmentPath));

    private static string BuildExclusionReason(string? departmentPath) =>
        string.IsNullOrWhiteSpace(NormalizeForCompare(departmentPath))
            ? "Chưa xác định được bộ phận hưởng phụ cấp độc hại."
            : "Bộ phận không thuộc diện hưởng phụ cấp độc hại.";

    private static string NormalizeForCompare(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var builder = new StringBuilder(value.Trim().Length);
        foreach (var character in value.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            builder.Append(character switch { 'đ' => 'd', 'Đ' => 'D', _ => character });
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
