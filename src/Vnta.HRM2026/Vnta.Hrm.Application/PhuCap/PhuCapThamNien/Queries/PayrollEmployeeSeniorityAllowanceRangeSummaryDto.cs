namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

/// <summary>
/// Số lượng bản ghi thuộc một khoảng thâm niên trong cùng ngữ nghĩa tìm kiếm của kỳ lương.
/// </summary>
public sealed record PayrollEmployeeSeniorityAllowanceRangeSummaryDto(
    string RangeKey,
    int Count);
