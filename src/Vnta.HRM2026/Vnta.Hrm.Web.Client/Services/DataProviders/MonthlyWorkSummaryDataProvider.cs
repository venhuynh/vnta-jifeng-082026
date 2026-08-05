using Vnta.Hrm.Application.Integrations.AttendanceGateway;
using Vnta.Hrm.Web.Client.Models;
using Vnta.Hrm.Web.Client.Models.Attendance;

namespace Vnta.Hrm.Web.Client.Services.DataProviders;

// Adapter UI chỉ đổi DTO đọc của Application thành record phục vụ grid; không giữ rule tính công.
public sealed class MonthlyWorkSummaryDataProvider(
    IAttendanceMonthlyWorkSummaryGridReadService attendanceMonthlyWorkSummaryGridReadService)
    : IMonthlyWorkSummaryDataProvider
{
    private const int DefaultPageSize = 50;

    public async Task<MonthlyWorkSummaryLoadResult> LoadPageAsync(
        MonthlyWorkSummaryPageRequest request,
        CancellationToken cancellationToken = default)
    {
        // Server vẫn là nơi chuẩn hóa filter, giới hạn take và phân trang; provider không tự query database.
        var page = await attendanceMonthlyWorkSummaryGridReadService.SearchAsync(
            new AttendanceMonthlyWorkSummaryGridFilter(
                request.FromDate,
                request.ToDate,
                NormalizeOptional(request.SearchText),
                request.Skip,
                request.Take <= 0 ? DefaultPageSize : request.Take,
                IncludeShiftDetails: false),
            cancellationToken);

        var mappedRows = page.Rows
            .Select(static row => MapRow(row, includeShiftDetails: false))
            .ToArray();

        return new MonthlyWorkSummaryLoadResult(
            mappedRows,
            page.TotalCount);
    }

    public async Task<MonthlyWorkSummaryGridRowRecord?> LoadEmployeeMonthAsync(
        DateOnly fromDate,
        DateOnly toDate,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var page = await attendanceMonthlyWorkSummaryGridReadService.SearchAsync(
            new AttendanceMonthlyWorkSummaryGridFilter(
                fromDate,
                toDate,
                null,
                Take: 1,
                EmployeeId: employeeId),
            cancellationToken);

        return page.Rows.Count == 0 ? null : MapRow(page.Rows[0], includeShiftDetails: true);
    }

    private static MonthlyWorkSummaryGridRowRecord MapRow(
        AttendanceMonthlyWorkSummaryGridRowDto row,
        bool includeShiftDetails)
    {
        // Khử trùng lặp phòng vệ cho một ngày: record mới nhất thắng để dictionary unbound chỉ có một ô cho mỗi DateOnly.
        var dayCells = row.DayCells
            .Select(dayCell => includeShiftDetails
                ? MapDayCellWithShiftDetails(dayCell)
                : MapDayCellWithoutShiftDetails(dayCell))
            .GroupBy(dayCell => dayCell.WorkDate)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(dayCell => dayCell.UpdatedAtUtc ?? dayCell.ComputedAtUtc)
                    .ThenByDescending(dayCell => dayCell.CreatedAtUtc)
                    .ThenByDescending(dayCell => dayCell.Id)
                    .First());

        return new MonthlyWorkSummaryGridRowRecord
        {
            Id = row.EmployeeId,
            RowNumber = row.RowNumber,
            EmployeeCode = NormalizeDisplayText(row.EmployeeCode) ?? "--",
            EmployeeName = NormalizeDisplayText(row.EmployeeName) ?? "--",
            DepartmentName = NormalizeDisplayText(row.DepartmentName) ?? "--",
            PositionName = NormalizeDisplayText(row.PositionName) ?? "--",
            DayCellsByDate = dayCells
        };
    }

    private static string? NormalizeDisplayText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // Luồng nút Xem của Bảng công tháng không hiển thị ca, nên không copy các field ca vào model UI.
    private static MonthlyWorkSummaryDayCellRecord MapDayCellWithoutShiftDetails(
        AttendanceMonthlyWorkSummaryDayCellDto dayCell) =>
        new()
        {
            Id = dayCell.Id,
            WorkDate = dayCell.WorkDate,
            DayType = dayCell.DayType,
            CheckInAt = dayCell.CheckInAt,
            CheckOutAt = dayCell.CheckOutAt,
            LateMinutes = dayCell.LateMinutes,
            EarlyLeaveMinutes = dayCell.EarlyLeaveMinutes,
            Status = dayCell.Status,
            IsLocked = dayCell.IsLocked,
            OvertimeMinutes = dayCell.OvertimeMinutes,
            OvertimeMinutes15 = dayCell.OvertimeMinutes15,
            OvertimeMinutes20 = dayCell.OvertimeMinutes20,
            OvertimeMinutes30 = dayCell.OvertimeMinutes30,
            ComputedAtUtc = dayCell.ComputedAtUtc,
            CreatedAtUtc = dayCell.CreatedAtUtc,
            UpdatedAtUtc = dayCell.UpdatedAtUtc
        };

    private static MonthlyWorkSummaryDayCellRecord MapDayCellWithShiftDetails(
        AttendanceMonthlyWorkSummaryDayCellDto dayCell) =>
        new()
        {
            Id = dayCell.Id,
            WorkDate = dayCell.WorkDate,
            DayType = dayCell.DayType,
            ShiftCode = dayCell.ShiftCode,
            ShiftShortName = dayCell.ShiftShortName,
            ShiftName = dayCell.ShiftName,
            ShiftColorHex = dayCell.ShiftColorHex,
            CheckInAt = dayCell.CheckInAt,
            CheckOutAt = dayCell.CheckOutAt,
            LateMinutes = dayCell.LateMinutes,
            EarlyLeaveMinutes = dayCell.EarlyLeaveMinutes,
            Status = dayCell.Status,
            IsLocked = dayCell.IsLocked,
            OvertimeMinutes = dayCell.OvertimeMinutes,
            OvertimeMinutes15 = dayCell.OvertimeMinutes15,
            OvertimeMinutes20 = dayCell.OvertimeMinutes20,
            OvertimeMinutes30 = dayCell.OvertimeMinutes30,
            ComputedAtUtc = dayCell.ComputedAtUtc,
            CreatedAtUtc = dayCell.CreatedAtUtc,
            UpdatedAtUtc = dayCell.UpdatedAtUtc
        };
}
