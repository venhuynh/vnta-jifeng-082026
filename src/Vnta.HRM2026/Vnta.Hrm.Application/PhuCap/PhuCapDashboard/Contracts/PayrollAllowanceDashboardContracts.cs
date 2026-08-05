// Kept in the existing application contract namespace for wire/endpoint compatibility;
// the source now lives under the canonical PhuCapDashboard feature folder.
namespace Vnta.Hrm.Application.PhuCap.PhuCapTongHop.Contracts;

/// <summary>
/// Điều kiện tải dashboard phụ cấp theo một kỳ lương. Chuỗi xu hướng và so sánh
/// luôn bao phủ từ tháng 01 đến kỳ được chọn; các trường còn lại được giữ và kiểm tra
/// theo HTTP contract hiện hành.
/// </summary>
public sealed record PayrollAllowanceDashboardFilter(
    int PayrollMonth,
    int PayrollYear,
    int HistoryMonthCount = 12,
    int DepartmentTake = 5);

/// <summary>Contract đọc dữ liệu đã tổng hợp dành riêng cho dashboard phụ cấp.</summary>
public interface IPayrollAllowanceDashboardReadService
{
    Task<PayrollAllowanceDashboardDto> GetDashboardAsync(
        PayrollAllowanceDashboardFilter filter,
        CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceDashboardBreakdownQueryService
{
    Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto>> GetAllowanceBreakdownAsync(
        PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceDashboardTrendQueryService
{
    Task<IReadOnlyList<PayrollAllowanceDashboardTrendPointDto>> GetTrendAsync(
        PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceDashboardMonthlyComparisonQueryService
{
    Task<IReadOnlyList<PayrollAllowanceDashboardAllowanceComparisonDto>> GetAllowanceMonthlyComparisonAsync(
        PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default);
}

public interface IPayrollAllowanceDashboardDepartmentComparisonQueryService
{
    Task<IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto>> GetDepartmentMonthlyComparisonAsync(
        PayrollAllowanceDashboardFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>Toàn bộ dữ liệu cần để dựng dashboard phụ cấp cho một kỳ lương.</summary>
public sealed record PayrollAllowanceDashboardDto(
    int PayrollMonth,
    int PayrollYear,
    PayrollAllowanceDashboardOverviewDto Overview,
    PayrollAllowanceDashboardOverviewDto PreviousPeriodOverview,
    IReadOnlyList<PayrollAllowanceDashboardAllowanceBreakdownDto> AllowanceBreakdown,
    IReadOnlyList<PayrollAllowanceDashboardTrendPointDto> Trend,
    IReadOnlyList<PayrollAllowanceDashboardDepartmentDto> TopDepartments,
    IReadOnlyList<PayrollAllowanceDashboardAllowanceComparisonDto> AllowanceMonthlyComparison,
    IReadOnlyList<PayrollAllowanceDashboardDepartmentTreeNodeDto> DepartmentMonthlyComparison);

/// <summary>Các KPI trạng thái và giá trị của snapshot phụ cấp.</summary>
public sealed record PayrollAllowanceDashboardOverviewDto(
    int TotalCount,
    int OpenCount,
    int LockedCount,
    decimal TotalAllowanceAmount);

/// <summary>Giá trị của một loại phụ cấp.</summary>
public sealed record PayrollAllowanceDashboardAllowanceBreakdownDto(
    string AllowanceType,
    decimal Amount);

/// <summary>Điểm dữ liệu tổng phụ cấp của một kỳ lương.</summary>
public sealed record PayrollAllowanceDashboardTrendPointDto(
    int PayrollMonth,
    int PayrollYear,
    int EmployeeCount,
    decimal TotalAllowanceAmount);

/// <summary>Tổng phụ cấp theo phòng ban trong một kỳ.</summary>
public sealed record PayrollAllowanceDashboardDepartmentDto(
    string DepartmentName,
    int EmployeeCount,
    decimal TotalAllowanceAmount);

/// <summary>Số tiền một loại phụ cấp theo các tháng từ đầu năm đến kỳ đang xem.</summary>
public sealed record PayrollAllowanceDashboardAllowanceComparisonDto(
    string AllowanceType,
    IReadOnlyList<PayrollAllowanceDashboardAllowanceMonthDto> Months);

/// <summary>Số tiền phụ cấp của một phòng ban theo các tháng từ đầu năm đến kỳ đang xem.</summary>
public sealed record PayrollAllowanceDashboardDepartmentTreeNodeDto(
    string DepartmentName,
    IReadOnlyList<PayrollAllowanceDashboardAllowanceMonthDto> Months,
    string Id = "",
    string? ParentId = null,
    int HierarchyLevel = 0)
{
    public decimal CurrentAmount => Months.LastOrDefault()?.Amount ?? 0m;

    public decimal ChangeFromPreviousMonth =>
        CurrentAmount - (Months.Count > 1 ? Months[^2].Amount : 0m);
}

/// <summary>Giá trị phụ cấp của một tháng trong bảng so sánh.</summary>
public sealed record PayrollAllowanceDashboardAllowanceMonthDto(
    int PayrollMonth,
    decimal Amount);

