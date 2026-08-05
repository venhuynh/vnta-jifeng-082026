using System.Globalization;

namespace Vnta.Hrm.Web.Client.Models.Payroll;

/// <summary>
/// Mô hình chỉ phục vụ hiển thị cho danh sách khấu trừ phí công đoàn.
/// Giá trị và trạng thái khóa luôn do dịch vụ phía máy chủ xác nhận.
/// </summary>
public sealed class PayrollUnionFeeDeductionRecord
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("vi-VN");

    public Guid Id { get; init; }
    public Guid EmployeeId { get; init; }
    public string? EmployeeCode { get; init; }
    public string? EmployeeName { get; init; }
    public string? DepartmentName { get; init; }
    public string? PositionName { get; init; }
    public int PayrollMonth { get; init; }
    public int PayrollYear { get; init; }
    public decimal DeductionAmount { get; init; }
    public bool IsSummaryLocked { get; init; }
    public bool IsLocked { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? UpdatedAtUtc { get; init; }

    public string EmployeeDisplay => JoinDisplay(EmployeeCode, EmployeeName) ?? "Chưa xác định nhân viên";
    public string DepartmentDisplay => Normalize(DepartmentName) ?? "Chưa có phòng ban";
    public string PositionDisplay => Normalize(PositionName) ?? "Chưa có chức vụ";
    public string PayrollPeriodDisplay => $"{PayrollMonth:00}/{PayrollYear}";
    public string DeductionAmountDisplay => DeductionAmount == 0m
        ? string.Empty
        : string.Format(DisplayCulture, "{0:N0} đ", DeductionAmount);
    public string LockStatusText => IsSummaryLocked
        ? "Kỳ đã khóa"
        : IsLocked ? "Đã khóa" : "Đang mở";
    public string LockActionText => IsLocked ? "Mở khóa" : "Khóa";

    private static string? JoinDisplay(string? code, string? name) => Normalize(code) is { } employeeCode
        && Normalize(name) is { } employeeName
            ? $"{employeeCode} - {employeeName}"
            : Normalize(name) ?? Normalize(code);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
