using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Persistence;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapPhepLe.Queries;

/// <summary>
/// Read model EF Core của Phụ cấp Phép - Lễ. Toàn bộ filter, sort và giới hạn
/// kết quả phải nằm trước projection DTO để Npgsql có thể dịch sang SQL.
/// </summary>
public sealed class DatabaseLeaveHolidayAllowanceReadService(ApplicationDbContext dbContext)
    : ILeaveHolidayAllowanceReadService
{
    #region Giới hạn truy vấn

    private const int MaxSearchResultLimit = 5000;

    #endregion

    #region Truy vấn danh sách

    public async Task<IReadOnlyList<LeaveHolidayAllowanceListItemDto>> SearchAsync(
        LeaveHolidayAllowanceFilter filter,
        CancellationToken cancellationToken = default)
    {
        if(filter.PayrollYear is < 1900 or > 2100 || filter.PayrollMonth is < 1 or > 12)
        {
            throw new InvalidOperationException("Kỳ dữ liệu Phép - Lễ không hợp lệ.");
        }

        // Chuẩn hóa một lần để mọi điều kiện tìm kiếm dùng cùng một giá trị.
        var searchText = NormalizeOptional(filter.SearchText);
        var searchPattern = searchText is null ? null : $"%{searchText}%";
        var take = Math.Clamp(filter.Take, 1, MaxSearchResultLimit);

        return await LeaveHolidayAllowanceReadProjection.CreateItemsForPeriod(
                dbContext,
                filter.PayrollYear,
                filter.PayrollMonth,
                searchPattern)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Tiện ích nội bộ

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    #endregion
}
