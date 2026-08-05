using Vnta.Hrm.Web.Client.Models.Payroll;

namespace Vnta.Hrm.Web.Client.Services.DataProviders.PhuCap.PhuCapTongHop;

/// <summary>
/// Chuyển đổi dữ liệu giữa màn hình tổng hợp phụ cấp và hợp đồng API.
/// Each operation depends only on its matching narrow application capability.
/// </summary>
public sealed class PayrollAllowanceSummaryDataProvider(
    IPayrollAllowanceSummaryReadService readService,
    IPayrollAllowanceSummaryExportService exportService,
    IPayrollAllowanceSummaryPreviousMonthSyncService previousMonthSyncService,
    IPayrollAllowanceSummaryRefreshService refreshService,
    IPayrollAllowanceSummaryManualAdjustmentService manualAdjustmentService,
    IPayrollAllowanceSummaryLockService lockService) : IPayrollAllowanceSummaryDataProvider
{
    #region Đọc dữ liệu hiển thị

    /// <summary>Lấy các chỉ số tổng quan theo bộ lọc đang áp dụng.</summary>
    public Task<PayrollAllowanceSummaryOverviewDto> GetSummaryAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default) =>
        readService.GetSummaryAsync(filter, cancellationToken);

    public async Task<PayrollAllowanceSummaryLoadResult> SearchAsync(
        PayrollAllowanceSummaryFilter filter,
        CancellationToken cancellationToken = default)
    {
        var page = await readService.SearchAsync(filter, cancellationToken);
        return new PayrollAllowanceSummaryLoadResult(
            page.Rows.Select(MapRecord).ToArray(),
            page.TotalCount);
    }

    #endregion

    #region Xuất dữ liệu

    /// <summary>Tải toàn bộ dữ liệu kỳ lương để xuất, không phụ thuộc phân trang hay dòng đang chọn.</summary>
    public async Task<IReadOnlyList<PayrollAllowanceSummaryExportRecord>> LoadAllForPeriodExportAsync(
        int payrollMonth,
        int payrollYear,
        PayrollAllowanceSummaryExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var rows = await exportService.ExportAsync(
            new PayrollAllowanceSummaryExportRequest(payrollYear, payrollMonth, format),
            cancellationToken);

        return rows.Select(MapExportRecord).ToArray();
    }

    #endregion

    #region Đồng bộ và làm mới

    /// <summary>Sao chép snapshot tổng hợp từ tháng trước sang kỳ đích.</summary>
    public Task<SyncPayrollAllowanceSummaryFromPreviousMonthResult> SyncFromPreviousMonthAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        CancellationToken cancellationToken = default) =>
        previousMonthSyncService.SyncFromPreviousMonthAsync(
            new SyncPayrollAllowanceSummaryFromPreviousMonthRequest(targetPayrollMonth, targetPayrollYear, Actor: null),
            cancellationToken);

    /// <summary>Tính lại một dòng hoặc toàn bộ kỳ từ các nguồn phụ cấp thành phần.</summary>
    public Task<RefreshPayrollAllowanceSummaryResult> RefreshAsync(
        int targetPayrollMonth,
        int targetPayrollYear,
        Guid? payrollAllowanceSummaryRecordId = null,
        CancellationToken cancellationToken = default) =>
        refreshService.RefreshAsync(
            new RefreshPayrollAllowanceSummaryRequest(
                targetPayrollMonth,
                targetPayrollYear,
                Actor: null,
                PayrollAllowanceSummaryRecordId: payrollAllowanceSummaryRecordId),
            cancellationToken);

    #endregion

    #region Thay đổi dữ liệu

    /// <summary>Cập nhật trạng thái khóa của một dòng và ánh xạ kết quả cho giao diện.</summary>
    public async Task<PayrollAllowanceSummaryRecord> SetLockStateAsync(
        Guid id,
        bool isLocked,
        DateTime? originalUpdatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var row = await lockService.SetLockStateAsync(
            new SetPayrollAllowanceSummaryLockStateRequest(id, isLocked, originalUpdatedAtUtc, Actor: null),
            cancellationToken);

        return MapRecord(row);
    }

    /// <summary>Khóa hoặc mở khóa các dòng trong kỳ, có hỗ trợ token đồng thời cho từng dòng.</summary>
    public Task<SetPayrollAllowanceSummaryBatchLockStateResult> SetLockStateBatchAsync(
        SetPayrollAllowanceSummaryBatchLockStateRequest request,
        CancellationToken cancellationToken = default) =>
        lockService.SetLockStateBatchAsync(request, cancellationToken);

    /// <summary>Lưu toàn bộ khoản phụ cấp và ghi chú được nhập tay của dòng chưa khóa.</summary>
    public async Task<PayrollAllowanceSummaryRecord> UpdateManualValuesAsync(
        UpdatePayrollAllowanceSummaryManualValuesRequest request,
        CancellationToken cancellationToken = default)
    {
        var row = await manualAdjustmentService.UpdateManualValuesAsync(
            request with { Actor = null },
            cancellationToken);

        return MapRecord(row);
    }

    #endregion

    #region Ánh xạ DTO sang model giao diện

    /// <summary>Chuyển DTO danh sách từ API thành record dùng bởi grid và popup.</summary>
    private static PayrollAllowanceSummaryRecord MapRecord(PayrollAllowanceSummaryListItemDto source) =>
        new()
        {
            Id = source.Id,
            EmployeeId = source.EmployeeId,
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            ResponsibilityAllowanceAmount = source.ResponsibilityAllowanceAmount,
            ResponsibilityOtherAllowanceAmount = source.ResponsibilityOtherAllowanceAmount,
            SeniorityAllowanceAmount = source.SeniorityAllowanceAmount,
            AttendanceAllowanceAmount = source.AttendanceAllowanceAmount,
            MealAllowanceAmount = source.MealAllowanceAmount,
            HazardAllowanceAmount = source.HazardAllowanceAmount,
            OtherAllowanceAmount = source.OtherAllowanceAmount,
            LeaveHolidayAllowanceAmount = source.LeaveHolidayAllowanceAmount,
            IsLocked = source.IsLocked,
            Note = source.Note,
            CreatedAtUtc = source.CreatedAtUtc,
            CreatedBy = source.CreatedBy,
            UpdatedAtUtc = source.UpdatedAtUtc,
            UpdatedBy = source.UpdatedBy
        };

    /// <summary>Chuyển DTO đã được backend giới hạn trường sang model phục vụ tạo tệp xuất.</summary>
    private static PayrollAllowanceSummaryExportRecord MapExportRecord(
        PayrollAllowanceSummaryExportRowDto source) =>
        new()
        {
            EmployeeCode = source.EmployeeCode,
            EmployeeName = source.EmployeeName,
            DepartmentName = source.DepartmentName,
            PositionName = source.PositionName,
            PayrollMonth = source.PayrollMonth,
            PayrollYear = source.PayrollYear,
            ResponsibilityAllowanceAmount = source.ResponsibilityAllowanceAmount,
            ResponsibilityOtherAllowanceAmount = source.ResponsibilityOtherAllowanceAmount,
            SeniorityAllowanceAmount = source.SeniorityAllowanceAmount,
            AttendanceAllowanceAmount = source.AttendanceAllowanceAmount,
            MealAllowanceAmount = source.MealAllowanceAmount,
            HazardAllowanceAmount = source.HazardAllowanceAmount,
            OtherAllowanceAmount = source.OtherAllowanceAmount,
            LeaveHolidayAllowanceAmount = source.LeaveHolidayAllowanceAmount,
            TotalAllowanceAmount = source.TotalAllowanceAmount,
            IsLocked = source.IsLocked,
            Note = source.Note
        };

    #endregion
}
