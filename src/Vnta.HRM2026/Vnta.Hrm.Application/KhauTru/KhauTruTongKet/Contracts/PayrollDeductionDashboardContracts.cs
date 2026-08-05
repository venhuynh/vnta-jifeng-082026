namespace Vnta.Hrm.Application.KhauTru.KhauTruTongHop;

/// <summary>Điều kiện tải dashboard khấu trừ theo kỳ lương.</summary>
public sealed record PayrollDeductionDashboardFilter(
    int PayrollMonth,
    int PayrollYear);

/// <summary>Contract đọc dữ liệu tổng kết dành riêng cho dashboard khấu trừ.</summary>
public interface IPayrollDeductionDashboardService
{
    Task<PayrollDeductionDashboardDto> GetDashboardAsync(
        PayrollDeductionDashboardFilter filter,
        CancellationToken cancellationToken = default);
}

/// <summary>Toàn bộ dữ liệu cần để dựng dashboard khấu trừ cho một kỳ lương.</summary>
public sealed record PayrollDeductionDashboardDto(
    int PayrollMonth,
    int PayrollYear,
    PayrollDeductionDashboardOverviewDto Overview,
    PayrollDeductionDashboardOverviewDto PreviousPeriodOverview,
    IReadOnlyList<PayrollDeductionDashboardDeductionBreakdownDto> DeductionBreakdown,
    IReadOnlyList<PayrollDeductionDashboardTrendPointDto> Trend,
    IReadOnlyList<PayrollDeductionDashboardDeductionComparisonDto> DeductionMonthlyComparison,
    IReadOnlyList<PayrollDeductionDashboardDepartmentTreeNodeDto> DepartmentMonthlyComparison);

/// <summary>Các KPI trạng thái và giá trị của snapshot khấu trừ.</summary>
public sealed record PayrollDeductionDashboardOverviewDto(
    int TotalCount,
    int OpenCount,
    int LockedCount,
    decimal TotalDeductionAmount);

/// <summary>Giá trị của một khoản khấu trừ.</summary>
public sealed record PayrollDeductionDashboardDeductionBreakdownDto(
    string DeductionType,
    decimal Amount);

/// <summary>Điểm dữ liệu tổng khấu trừ của một kỳ lương.</summary>
public sealed record PayrollDeductionDashboardTrendPointDto(
    int PayrollMonth,
    int PayrollYear,
    int EmployeeCount,
    decimal TotalDeductionAmount);

/// <summary>Số tiền một khoản khấu trừ theo các tháng từ đầu năm đến kỳ đang xem.</summary>
public sealed record PayrollDeductionDashboardDeductionComparisonDto(
    string DeductionType,
    IReadOnlyList<PayrollDeductionDashboardMonthDto> Months);

/// <summary>Số tiền khấu trừ của một đơn vị theo các tháng từ đầu năm đến kỳ đang xem.</summary>
public sealed record PayrollDeductionDashboardDepartmentTreeNodeDto(
    string DepartmentName,
    IReadOnlyList<PayrollDeductionDashboardMonthDto> Months,
    string Id = "",
    string? ParentId = null,
    int HierarchyLevel = 0);

/// <summary>Giá trị khấu trừ của một tháng trong báo cáo so sánh.</summary>
public sealed record PayrollDeductionDashboardMonthDto(
    int PayrollMonth,
    decimal Amount);
