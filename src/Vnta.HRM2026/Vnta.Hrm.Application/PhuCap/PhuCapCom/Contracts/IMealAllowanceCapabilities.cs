namespace Vnta.Hrm.Application.PhuCap.PhuCapCom.Contracts;

/// <summary>Cung cấp dữ liệu phụ cấp cơm đã lọc và phân trang, không thay đổi snapshot.</summary>
public interface IMealAllowanceReadService
{
    Task<IReadOnlyList<MealAllowanceListItemDto>> SearchAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default);
    Task<MealAllowancePageDto> SearchPageAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default);
    Task<MealAllowanceSummaryDto> GetSummaryAsync(MealAllowanceFilter filter, CancellationToken cancellationToken = default);
}

/// <summary>Xuất snapshot phụ cấp cơm của một kỳ lương.</summary>
public interface IMealAllowanceExportService
{
    Task<IReadOnlyList<MealAllowanceListItemDto>> ExportPeriodAsync(
        int payrollMonth,
        int payrollYear,
        CancellationToken cancellationToken = default);
}

/// <summary>Khởi tạo hoặc tính lại snapshot từ dữ liệu công của kỳ lương.</summary>
public interface IMealAllowanceRefreshService
{
    Task<RefreshMealAllowanceResult> RefreshAsync(RefreshMealAllowanceRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Thay đổi trạng thái khóa theo dòng hoặc toàn kỳ lương.</summary>
public interface IMealAllowanceLockService
{
    Task<SetMealAllowanceLockStateBatchResult> SetLockStateBatchAsync(SetMealAllowanceLockStateBatchRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Điều chỉnh thủ công một snapshot đang mở, có kiểm tra phiên bản đồng thời.</summary>
public interface IMealAllowanceManualAdjustmentService
{
    Task<MealAllowanceListItemDto> UpdateManualValuesAsync(UpdateMealAllowanceManualValuesRequest request, CancellationToken cancellationToken = default);
}
