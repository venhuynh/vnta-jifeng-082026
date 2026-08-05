using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.ChamCong.CodeKetQuaTinhCong;

public sealed class DatabaseAttendanceStatusCodeService(
    ApplicationDbContext dbContext,
    ILogger<DatabaseAttendanceStatusCodeService> logger)
    : IAttendanceStatusCodeService
{
    public async Task<IReadOnlyList<AttendanceStatusCodeListItemDto>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await dbContext.AttendanceStatusCodes
                .AsNoTracking()
                .OrderBy(row => row.Code)
                .ThenBy(row => row.Name)
                .ThenBy(row => row.Kind)
                .Select(row => new AttendanceStatusCodeListItemDto(
                    row.Id,
                    row.Code,
                    row.Name,
                    row.Kind,
                    row.CongTangCa,
                    row.CongHanhChinh,
                    row.PhuCapTrachNhiemTinhNangSuat,
                    row.PhuCapDocHai,
                    row.PhuCapTrachNhiemKhac,
                    row.PhuCapPhepLe,
                    row.PhuCapTrachNhiemKhongTinhNangSuat,
                    row.PhuCapChuyenCan,
                    row.PhuCapThamNien,
                    row.KhauTruTamUng,
                    row.IsActive,
                    row.Note,
                    row.CreatedAtUtc,
                    row.UpdatedAtUtc))
                .ToListAsync(cancellationToken);
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
        {
            logger.LogError(
                ex,
                "attendance_status_codes is unavailable while reading the result-code catalog.");
            throw new AttendanceStatusCodeCatalogUnavailableException(
                "Danh mục code kết quả tính công chưa sẵn sàng.",
                ex);
        }
    }

    public async Task<AttendanceStatusCodeListItemDto> UpdateFlagsAsync(
        UpdateAttendanceStatusCodeFlagsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Mã định danh mã kết quả tính công không hợp lệ.");
        }

        if (request.OriginalUpdatedAtUtc == default)
        {
            throw new InvalidOperationException("Thiếu phiên bản dữ liệu để cập nhật mã kết quả tính công.");
        }

        var row = await dbContext.AttendanceStatusCodes
            .SingleOrDefaultAsync(item => item.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mã kết quả tính công cần cập nhật.");

        var currentVersion = row.UpdatedAtUtc ?? row.CreatedAtUtc;
        if (currentVersion != request.OriginalUpdatedAtUtc)
        {
            throw new AttendanceStatusCodeConflictException(
                "Mã kết quả tính công đã được cập nhật bởi người dùng khác. Vui lòng tải lại và thử lại.");
        }

        row.CongHanhChinh = request.CongHanhChinh;
        row.PhuCapTrachNhiemTinhNangSuat = request.PhuCapTrachNhiemTinhNangSuat;
        row.PhuCapDocHai = request.PhuCapDocHai;
        row.PhuCapTrachNhiemKhac = request.PhuCapTrachNhiemKhac;
        row.PhuCapPhepLe = request.PhuCapPhepLe;
        row.PhuCapTrachNhiemKhongTinhNangSuat = request.PhuCapTrachNhiemKhongTinhNangSuat;
        row.PhuCapChuyenCan = request.PhuCapChuyenCan;
        row.PhuCapThamNien = request.PhuCapThamNien;
        row.KhauTruTamUng = request.KhauTruTamUng;
        row.UpdatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapListItem(row);
    }

    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Mã định danh mã kết quả tính công không hợp lệ.");
        }

        var row = await dbContext.AttendanceStatusCodes
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mã kết quả tính công cần xóa.");

        var isReferencedByWorkdaySummary = await dbContext.AttendanceWorkdaySummaries
            .AsNoTracking()
            .AnyAsync(summary => summary.CodeKetQuaTinhCongId == id, cancellationToken);

        if (isReferencedByWorkdaySummary)
        {
            throw new InvalidOperationException(
                "Không thể xóa mã kết quả tính công vì mã này đang được sử dụng trong bảng công hằng ngày.");
        }

        dbContext.AttendanceStatusCodes.Remove(row);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresException
                                            && postgresException.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            throw new InvalidOperationException(
                "Không thể xóa mã kết quả tính công vì mã này đang được sử dụng trong bảng công hằng ngày.",
                ex);
        }
    }

    private static AttendanceStatusCodeListItemDto MapListItem(AttendanceStatusCodeRow row) =>
        new(
            row.Id,
            row.Code,
            row.Name,
            row.Kind,
            row.CongTangCa,
            row.CongHanhChinh,
            row.PhuCapTrachNhiemTinhNangSuat,
            row.PhuCapDocHai,
            row.PhuCapTrachNhiemKhac,
            row.PhuCapPhepLe,
            row.PhuCapTrachNhiemKhongTinhNangSuat,
            row.PhuCapChuyenCan,
            row.PhuCapThamNien,
            row.KhauTruTamUng,
            row.IsActive,
            row.Note,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);
}
