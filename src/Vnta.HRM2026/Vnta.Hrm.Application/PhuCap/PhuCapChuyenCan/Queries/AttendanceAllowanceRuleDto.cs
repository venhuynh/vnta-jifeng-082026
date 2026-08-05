using Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Policies;

namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

/// <summary>
/// Rule metadata dùng để giải thích cách tính phụ cấp chuyên cần trên UI.
/// Danh sách mã CTL được lấy từ cấu hình chấm công, không phải từ UI.
/// </summary>
public sealed record AttendanceAllowanceRuleDto(
    IReadOnlyList<string> EligibleStatusCodes,
    AttendanceAllowanceRuleMetadataDto Metadata)
{
    /// <summary>Preserves callers that previously supplied only configured status codes.</summary>
    public AttendanceAllowanceRuleDto(IReadOnlyList<string> eligibleStatusCodes)
        : this(eligibleStatusCodes, AttendanceAllowanceRuleMetadataDto.Current)
    {
    }
}

/// <summary>
/// Server-owned constants used to explain the currently effective attendance-allowance policy.
/// Presentation clients consume this payload instead of duplicating policy values.
/// </summary>
public sealed record AttendanceAllowanceRuleMetadataDto(
    int MinimumSupportedPayrollMonth,
    int MinimumSupportedPayrollYear,
    int MaximumSupportedPayrollYear,
    int LateEarlyMinutesPerWorkday,
    int WorkdayDecimalPlaces,
    int CalculationDecimalPlaces,
    string KpAttendanceStatusCode,
    decimal AttendanceClassAMissingWorkdayThreshold,
    decimal AttendanceClassBMissingWorkdayThreshold,
    decimal AttendanceClassAAmount,
    decimal AttendanceClassBAmount,
    decimal AttendanceClassCAmount)
{
    public static AttendanceAllowanceRuleMetadataDto Current { get; } = new(
        AttendanceAllowancePayrollPeriodPolicy.MinimumSupportedMonth,
        AttendanceAllowancePayrollPeriodPolicy.MinimumSupportedYear,
        AttendanceAllowancePayrollPeriodPolicy.MaximumSupportedYear,
        AttendanceAllowanceWorkdayMetricPolicy.LateEarlyMinutesPerWorkday,
        AttendanceAllowanceWorkdayMetricPolicy.WorkdayDecimalPlaces,
        AttendanceAllowanceCalculationPolicy.CalculationDecimalPlaces,
        AttendanceAllowanceWorkdayMetricPolicy.KpAttendanceStatusCode,
        AttendanceAllowanceCalculationPolicy.AttendanceClassAMissingWorkdayThreshold,
        AttendanceAllowanceCalculationPolicy.AttendanceClassBMissingWorkdayThreshold,
        AttendanceAllowanceCalculationPolicy.AttendanceClassAAmount,
        AttendanceAllowanceCalculationPolicy.AttendanceClassBAmount,
        AttendanceAllowanceCalculationPolicy.AttendanceClassCAmount);
}
