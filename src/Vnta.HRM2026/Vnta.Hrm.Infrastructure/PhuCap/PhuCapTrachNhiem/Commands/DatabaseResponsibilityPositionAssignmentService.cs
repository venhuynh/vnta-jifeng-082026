using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

/// <summary>
/// Implements the position-to-responsibility-grade configuration use cases for the
/// Interactive Server screen.  It intentionally does not expose the broader
/// responsibility-allowance workflow so each operation has a small, auditable scope.
/// </summary>
/// <summary>
/// Shared persistence operations for position-to-grade mappings. Focused services
/// below expose read, write, copy and export contracts separately.
/// </summary>
public abstract class ResponsibilityPositionAssignmentPersistenceOperations(ApplicationDbContext dbContext)
{
    /// <summary>Mốc kỳ đầu tiên mà cấu hình ánh xạ phụ cấp trách nhiệm được cho phép.</summary>
    private static readonly ResponsibilityAllowancePeriod MinimumSupportedPeriod = new(2026, 6);
    private const int MaximumSupportedYear = 2100;
    private const int MaximumPageSize = 10_000;
    private const int MaximumExportRows = 10_000;
    private const int MaximumNoteLength = 500;

    /// <summary>
    /// Trả một trang mapping chức vụ-bậc theo kỳ, có tìm kiếm và giới hạn kích
    /// thước trang để tránh truy vấn không kiểm soát từ giao diện.
    /// </summary>
    public async Task<ResponsibilityPositionAssignmentPageDto> SearchPageAsync(
        ResponsibilityPositionAssignmentQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(query.Year, query.Month);

        // Chuẩn hóa phân trang từ client: skip âm về 0 và take bị giới hạn theo ngưỡng an toàn.
        var skip = Math.Max(query.Skip, 0);
        var take = query.Take <= 0
            ? 100
            : Math.Min(query.Take, MaximumPageSize);
        var filteredMappings = BuildFilteredMappingsQuery(query.Year, query.Month, query.SearchText);

        // Đếm trên cùng truy vấn lọc để pager không bị lệch với dữ liệu trả về.
        var totalCount = await filteredMappings.CountAsync(cancellationToken);
        var rows = await ProjectItems(filteredMappings)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return new ResponsibilityPositionAssignmentPageDto(rows, totalCount);
    }

    /// <summary>
    /// Đọc các bậc của đúng kỳ làm dữ liệu chọn cho biểu mẫu mapping. Vẫn trả cả
    /// bậc ngừng dùng để UI có thể hiển thị chính xác lịch sử bản ghi hiện hữu.
    /// </summary>
    public async Task<IReadOnlyList<ResponsibilityPositionAssignmentGradeOptionDto>> GetGradeOptionsAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        return await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(row => row.Year == year && row.Month == month)
            .OrderBy(row => row.DisplayOrder)
            .ThenBy(row => row.Code)
            .Select(row => new ResponsibilityPositionAssignmentGradeOptionDto(
                row.Id,
                row.Year,
                row.Month,
                row.Code,
                row.Name,
                row.StandardResponsibilityAllowanceAmount,
                row.DisplayOrder,
                row.IsActive,
                row.Note))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Tạo hoặc cập nhật mapping chức vụ-bậc. Một chức vụ chỉ có một mapping mỗi
    /// kỳ; bản ghi cập nhật dùng mốc thời gian để chống ghi đè đồng thời.
    /// </summary>
    public async Task<ResponsibilityPositionAssignmentItemDto> SaveAsync(
        SaveResponsibilityPositionAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);
        ValidateRequiredId(request.GradeId, "Cấp bậc trách nhiệm");
        ValidateRequiredId(request.PositionId, "Chức vụ");

        // Mapping chỉ hợp lệ khi bậc thuộc đúng kỳ; không cho liên kết bậc của kỳ khác.
        var grade = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .SingleOrDefaultAsync(
                row => row.Id == request.GradeId
                    && row.Year == request.Year
                    && row.Month == request.Month,
                cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy cấp bậc trách nhiệm của kỳ đã chọn.");

        var position = await dbContext.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == request.PositionId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy chức vụ đã chọn.");

        var now = GetDatabaseNow();
        PayrollResponsibilityAllowanceGradePositionRow mapping;
        var isNew = !request.Id.HasValue || request.Id.Value == Guid.Empty;

        // Nhánh tạo mới kiểm tra unique ở mức nghiệp vụ trước, còn database là lớp bảo vệ cuối cùng.
        if (isNew)
        {
            var existing = await dbContext.PayrollResponsibilityAllowanceGradePositions
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    row => row.Year == request.Year
                        && row.Month == request.Month
                        && row.PositionId == request.PositionId,
                    cancellationToken);
            if (existing is not null)
            {
                throw new ResponsibilityPositionAssignmentConflictException(
                    "Chức vụ này đã được gán cấp bậc trong kỳ đang chọn. Hãy tải lại dữ liệu để xem thay đổi mới nhất.");
            }

            mapping = new PayrollResponsibilityAllowanceGradePositionRow
            {
                Id = Guid.NewGuid(),
                Year = request.Year,
                Month = request.Month,
                CreatedAtUtc = now
            };
            dbContext.PayrollResponsibilityAllowanceGradePositions.Add(mapping);
        }
        else
        {
            mapping = await dbContext.PayrollResponsibilityAllowanceGradePositions
                .SingleOrDefaultAsync(row => row.Id == request.Id!.Value, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy gán chức vụ cần cập nhật. Hãy tải lại dữ liệu kỳ hiện tại.");

            if (mapping.Year != request.Year || mapping.Month != request.Month)
            {
                throw new InvalidOperationException("Không thể chuyển gán chức vụ đang sửa sang kỳ khác. Hãy đóng biểu mẫu và tải lại dữ liệu.");
            }

            // Token timestamp bảo vệ bản ghi khi hai người dùng cùng sửa mapping.
            EnsureUnchanged(mapping, request.OriginalUpdatedAtUtc);

            if (mapping.PositionId != request.PositionId)
            {
                var hasPositionConflict = await dbContext.PayrollResponsibilityAllowanceGradePositions
                    .AsNoTracking()
                    .AnyAsync(
                        row => row.Id != mapping.Id
                            && row.Year == request.Year
                            && row.Month == request.Month
                            && row.PositionId == request.PositionId,
                        cancellationToken);
                if (hasPositionConflict)
                {
                    throw new ResponsibilityPositionAssignmentConflictException(
                        "Chức vụ này đã được gán cấp bậc trong kỳ đang chọn. Hãy tải lại dữ liệu để xem thay đổi mới nhất.");
                }
            }
        }

        mapping.GradeId = grade.Id;
        mapping.PositionId = position.Id;
        mapping.IsActive = request.IsActive;
        mapping.Note = NormalizeNote(request.Note);
        mapping.UpdatedAtUtc = now;

        await SaveChangesAsConflictAsync(cancellationToken);
        return MapItem(mapping, grade, position);
    }

    /// <summary>
    /// Ngừng hiệu lực mapping của kỳ theo soft deactivate. Không xóa record để
    /// giữ nguyên lịch sử cấu hình và các snapshot đã được tính từ nó.
    /// </summary>
    public async Task<ResponsibilityPositionAssignmentItemDto> DeactivateAsync(
        DeactivateResponsibilityPositionAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequiredId(request.Id, "Gán chức vụ");
        ValidatePeriod(request.Year, request.Month);

        var mapping = await dbContext.PayrollResponsibilityAllowanceGradePositions
            .SingleOrDefaultAsync(
                row => row.Id == request.Id
                    && row.Year == request.Year
                    && row.Month == request.Month,
                cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy gán chức vụ của kỳ đang chọn. Hãy tải lại dữ liệu.");

        EnsureUnchanged(mapping, request.OriginalUpdatedAtUtc);

        if (!mapping.IsActive)
        {
            throw new InvalidOperationException("Gán chức vụ này đã ngừng dùng. Hãy tải lại danh sách trước khi thao tác tiếp.");
        }

        var grade = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == mapping.GradeId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy cấp bậc của gán chức vụ đã chọn.");
        var position = await dbContext.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == mapping.PositionId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy chức vụ của gán chức vụ đã chọn.");

        mapping.IsActive = false;
        mapping.UpdatedAtUtc = GetDatabaseNow();
        await SaveChangesAsConflictAsync(cancellationToken);

        return MapItem(mapping, grade, position);
    }

    /// <summary>
    /// Sao chép mapping của tháng trước sang kỳ đích bằng mã bậc (không dùng ID
    /// giữa hai kỳ). Transaction Serializable bảo vệ toàn kỳ đích khỏi thao tác
    /// lưu/ngừng dùng chồng lấp; mapping không tìm thấy bậc tương ứng bị bỏ qua.
    /// </summary>
    public async Task<CopyResponsibilityPositionAssignmentsResult> CopyFromPreviousPeriodAsync(
        CopyResponsibilityPositionAssignmentsRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        // Luôn sao chép từ kỳ liền trước, bao gồm cả biên tháng 01 sang tháng 12 năm trước.
        var previousPeriod = GetPreviousPeriod(request.Year, request.Month);
        // The copy command reads and rewrites a whole target period. Serializable
        // isolation prevents an overlapping save/deactivate from being silently
        // overwritten by the bulk operation.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var sourceMappings = await (
                from mapping in dbContext.PayrollResponsibilityAllowanceGradePositions.AsNoTracking()
                join grade in dbContext.PayrollResponsibilityAllowanceGrades.AsNoTracking()
                    on mapping.GradeId equals grade.Id
                where mapping.Year == previousPeriod.Year
                    && mapping.Month == previousPeriod.Month
                    && grade.Year == previousPeriod.Year
                    && grade.Month == previousPeriod.Month
                select new CopySourceMapping(mapping.PositionId, grade.Code, mapping.IsActive, mapping.Note))
            .ToListAsync(cancellationToken);

        if (sourceMappings.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return new CopyResponsibilityPositionAssignmentsResult(
                request.Year,
                request.Month,
                previousPeriod.Year,
                previousPeriod.Month,
                0,
                0,
                0,
                0);
        }

        var targetGrades = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(grade => grade.Year == request.Year && grade.Month == request.Month)
            .Select(grade => new { grade.Id, grade.Code })
            .ToListAsync(cancellationToken);
        // Không tái dùng GradeId của tháng trước; liên kết lại bằng Code để đúng dữ liệu kỳ đích.
        var targetGradesByCode = targetGrades
            .GroupBy(grade => grade.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.OrdinalIgnoreCase);
        var targetMappingsByPositionId = await dbContext.PayrollResponsibilityAllowanceGradePositions
            .Where(mapping => mapping.Year == request.Year && mapping.Month == request.Month)
            .ToDictionaryAsync(mapping => mapping.PositionId, cancellationToken);

        var now = GetDatabaseNow();
        var createdCount = 0;
        var updatedCount = 0;
        var skippedMissingGradeCount = 0;

        // ID bậc thay đổi theo kỳ nên phải tái liên kết từ mã bậc tại kỳ đích.
        foreach (var source in sourceMappings)
        {
            if (!targetGradesByCode.TryGetValue(source.GradeCode, out var targetGradeId))
            {
                skippedMissingGradeCount++;
                continue;
            }

            if (targetMappingsByPositionId.TryGetValue(source.PositionId, out var target))
            {
                target.GradeId = targetGradeId;
                target.IsActive = source.IsActive;
                target.Note = NormalizeNote(source.Note);
                target.UpdatedAtUtc = now;
                updatedCount++;
                continue;
            }

            target = new PayrollResponsibilityAllowanceGradePositionRow
            {
                Id = Guid.NewGuid(),
                Year = request.Year,
                Month = request.Month,
                GradeId = targetGradeId,
                PositionId = source.PositionId,
                IsActive = source.IsActive,
                Note = NormalizeNote(source.Note),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            dbContext.PayrollResponsibilityAllowanceGradePositions.Add(target);
            targetMappingsByPositionId[source.PositionId] = target;
            createdCount++;
        }

        await SaveChangesAsConflictAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CopyResponsibilityPositionAssignmentsResult(
            request.Year,
            request.Month,
            previousPeriod.Year,
            previousPeriod.Month,
            sourceMappings.Count,
            createdCount,
            updatedCount,
            skippedMissingGradeCount);
    }

    /// <summary>
    /// Lấy toàn bộ mapping để xuất file, có mức trần an toàn. Đọc một dòng vượt
    /// trần để báo lỗi rõ ràng thay vì xuất một tệp bị cắt ngầm.
    /// </summary>
    public async Task<IReadOnlyList<ResponsibilityPositionAssignmentExportItemDto>> ExportAllAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        var rows = await ProjectItems(BuildFilteredMappingsQuery(year, month, null))
            .Select(row => new ResponsibilityPositionAssignmentExportItemDto(
                row.Id,
                row.Year,
                row.Month,
                row.PositionCode,
                row.PositionName,
                row.GradeCode,
                row.GradeName,
                row.IsActive ? "Đang dùng" : "Ngừng",
                row.Note))
            .Take(MaximumExportRows + 1)
            .ToListAsync(cancellationToken);

        if (rows.Count > MaximumExportRows)
        {
            throw new InvalidOperationException(
                $"Dữ liệu xuất vượt quá {MaximumExportRows:N0} dòng. Hãy thu hẹp kỳ hoặc dùng chức năng xuất nền.");
        }

        return rows;
    }

    /// <summary>
    /// Dựng truy vấn mapping no-tracking và, khi có từ khóa, tìm không phân biệt
    /// hoa thường trên ghi chú, mã/tên chức vụ và mã/tên bậc bằng PostgreSQL ILike.
    /// </summary>
    private IQueryable<PayrollResponsibilityAllowanceGradePositionRow> BuildFilteredMappingsQuery(
        int year,
        int month,
        string? searchText)
    {
        var query = dbContext.PayrollResponsibilityAllowanceGradePositions
            .AsNoTracking()
            .Where(mapping => mapping.Year == year && mapping.Month == month);

        // Không có từ khóa thì trả truy vấn cơ sở để EF sinh SQL đơn giản nhất.
        var normalizedSearch = NormalizeSearchText(searchText);
        if (normalizedSearch is null)
        {
            return query;
        }

        // ILike là tìm kiếm không phân biệt hoa thường của PostgreSQL; % tạo match chứa chuỗi.
        var pattern = $"%{normalizedSearch}%";
        return query.Where(mapping =>
            EF.Functions.ILike(mapping.Note ?? string.Empty, pattern)
            || dbContext.Positions.Any(position =>
                position.Id == mapping.PositionId
                && (EF.Functions.ILike(position.Code, pattern)
                    || EF.Functions.ILike(position.Name, pattern)))
            || dbContext.PayrollResponsibilityAllowanceGrades.Any(grade =>
                grade.Id == mapping.GradeId
                && grade.Year == year
                && grade.Month == month
                && (EF.Functions.ILike(grade.Code, pattern)
                    || EF.Functions.ILike(grade.Name, pattern))));
    }

    /// <summary>
    /// Ghép dữ liệu gán với ngạch và chức vụ, đồng thời sắp xếp trên các cột entity
    /// trước khi tạo DTO. EF Core không thể dịch phép sắp xếp trên constructor DTO
    /// của truy vấn này sang SQL.
    /// </summary>
    private IQueryable<ResponsibilityPositionAssignmentItemDto> ProjectItems(
        IQueryable<PayrollResponsibilityAllowanceGradePositionRow> mappings)
    {
        var joinedItems =
            from mapping in mappings
            join grade in dbContext.PayrollResponsibilityAllowanceGrades.AsNoTracking()
                on mapping.GradeId equals grade.Id
            join position in dbContext.Positions.AsNoTracking()
                on mapping.PositionId equals position.Id
            select new { mapping, grade, position };

        return joinedItems
            .OrderBy(row => row.position.Code)
            .ThenBy(row => row.position.Name)
            .ThenBy(row => row.grade.Code)
            .Select(row => new ResponsibilityPositionAssignmentItemDto(
                row.mapping.Id,
                row.mapping.Year,
                row.mapping.Month,
                row.grade.Id,
                row.grade.Code,
                row.grade.Name,
                row.position.Id,
                row.position.Code,
                row.position.Name,
                row.mapping.IsActive,
                row.mapping.Note,
                row.mapping.CreatedAtUtc,
                row.mapping.UpdatedAtUtc));
    }

    /// <summary>Ánh xạ các entity đã tải sang DTO chi tiết dùng cho phản hồi command.</summary>
    private static ResponsibilityPositionAssignmentItemDto MapItem(
        PayrollResponsibilityAllowanceGradePositionRow mapping,
        PayrollResponsibilityAllowanceGradeRow grade,
        NhanSu.ChucVu.AttendanceGatewayPositionRow position) =>
        new(
            mapping.Id,
            mapping.Year,
            mapping.Month,
            grade.Id,
            grade.Code,
            grade.Name,
            position.Id,
            position.Code,
            position.Name,
            mapping.IsActive,
            mapping.Note,
            mapping.CreatedAtUtc,
            mapping.UpdatedAtUtc);

    /// <summary>
    /// Lưu thay đổi và chuẩn hóa lỗi unique/serialization của PostgreSQL thành lỗi
    /// xung đột nghiệp vụ; tracker được xóa để không giữ entity ở trạng thái lỗi.
    /// </summary>
    private async Task SaveChangesAsConflictAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException
            && postgresException.SqlState is PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.SerializationFailure)
        {
            dbContext.ChangeTracker.Clear();
            throw new ResponsibilityPositionAssignmentConflictException(
                "Dữ liệu đã được thay đổi bởi thao tác khác. Hãy tải lại danh sách rồi thực hiện lại.");
        }
    }

    /// <summary>So sánh phiên bản client gửi với phiên bản hiện tại của mapping.</summary>
    private static void EnsureUnchanged(
        PayrollResponsibilityAllowanceGradePositionRow mapping,
        DateTime? originalUpdatedAtUtc)
    {
        var currentVersion = mapping.UpdatedAtUtc ?? mapping.CreatedAtUtc;
        var expectedVersion = originalUpdatedAtUtc ?? mapping.CreatedAtUtc;
        if (currentVersion != expectedVersion)
        {
            throw new ResponsibilityPositionAssignmentConflictException(
                "Gán chức vụ đã được thay đổi bởi người dùng khác. Hãy tải lại dữ liệu trước khi tiếp tục.");
        }
    }

    /// <summary>Trim ghi chú và áp giới hạn 500 ký tự trước khi persistence.</summary>
    private static string? NormalizeNote(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > MaximumNoteLength)
        {
            throw new InvalidOperationException($"Ghi chú không được vượt quá {MaximumNoteLength} ký tự.");
        }

        return normalized;
    }

    /// <summary>Chuẩn hóa từ khóa; chuỗi trắng có nghĩa không lọc.</summary>
    private static string? NormalizeSearchText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Chặn GUID rỗng cho các trường bắt buộc trong command.</summary>
    private static void ValidateRequiredId(Guid value, string displayName)
    {
        if (value == Guid.Empty)
        {
            throw new InvalidOperationException($"{displayName} là bắt buộc.");
        }
    }

    /// <summary>Kiểm tra kỳ cấu hình thuộc phạm vi module và có tháng hợp lệ.</summary>
    private static void ValidatePeriod(int year, int month)
    {
        if (year < MinimumSupportedPeriod.Year || year > MaximumSupportedYear || month is < 1 or > 12)
        {
            throw new InvalidOperationException("Kỳ cấu hình phụ cấp trách nhiệm không hợp lệ.");
        }

        if (year == MinimumSupportedPeriod.Year && month < MinimumSupportedPeriod.Month)
        {
            throw new InvalidOperationException("Chỉ hỗ trợ dữ liệu phụ cấp trách nhiệm từ kỳ 06/2026.");
        }
    }

    /// <summary>Trả kỳ liền trước, xử lý biên tháng 1 sang tháng 12 của năm trước.</summary>
    private static ResponsibilityAllowancePeriod GetPreviousPeriod(int year, int month) =>
        month == 1
            ? new ResponsibilityAllowancePeriod(year - 1, 12)
            : new ResponsibilityAllowancePeriod(year, month - 1);

    /// <summary>Trả thời điểm nghiệp vụ UTC+7 dạng Unspecified theo quy ước database.</summary>
    private static DateTime GetDatabaseNow() =>
        DateTime.SpecifyKind(DateTime.UtcNow.AddHours(7), DateTimeKind.Unspecified);

    private readonly record struct ResponsibilityAllowancePeriod(int Year, int Month);

    private sealed record CopySourceMapping(
        Guid PositionId,
        string GradeCode,
        bool IsActive,
        string? Note);
}
