namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Commands;

/// <summary>
/// Số liệu kết quả của một lần làm mới, bao gồm quy mô nguồn dữ liệu và số snapshot được tạo, cập nhật hoặc bỏ qua vì đã khóa.
/// </summary>
public sealed record RefreshPayrollAllowanceSummaryResult(
    int TargetPayrollMonth,
    int TargetPayrollYear,
    int SourceEmployeeCount,
    int ResponsibilitySourceCount,
    int SenioritySourceCount,
    int AttendanceSourceCount,
    int MealSourceCount,
    int HazardSourceCount,
    int OtherAllowanceSourceCount,
    int OtherResponsibilitySourceCount,
    int LeaveHolidaySourceCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedLockedCount);
