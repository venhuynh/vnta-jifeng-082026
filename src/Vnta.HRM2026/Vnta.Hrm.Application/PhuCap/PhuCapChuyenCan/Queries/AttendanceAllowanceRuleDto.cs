namespace Vnta.Hrm.Application.PhuCap.PhuCapChuyenCan.Queries;

/// <summary>
/// Rule metadata dùng để giải thích cách tính phụ cấp chuyên cần trên UI.
/// Danh sách mã CTL được lấy từ cấu hình chấm công, không phải từ UI.
/// </summary>
public sealed record AttendanceAllowanceRuleDto(
    IReadOnlyList<string> EligibleStatusCodes);
