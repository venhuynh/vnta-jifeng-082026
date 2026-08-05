using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.Integrations.AttendanceGateway;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiem;

public abstract partial class PayrollResponsibilityAllowancePersistenceOperations
{
    #region Workflow cấu hình bậc trách nhiệm

    /// <summary>
    /// Tải cấu hình hoàn chỉnh của một kỳ: các bậc, ánh xạ bậc-chức vụ và gán
    /// riêng nhân viên. Tất cả phần đọc dùng no-tracking vì không thay đổi dữ liệu.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceGradeConfigDto> GetGradeConfigAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);

        // Tải từng mảng cấu hình độc lập để UI có thể hiển thị cả mapping/assignment không còn hoàn chỉnh.
        var grades = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .Where(x => x.Year == year && x.Month == month)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Code)
            .Select(x => new PayrollResponsibilityAllowanceGradeDto(
                x.Id,
                x.Year,
                x.Month,
                x.Code,
                x.Name,
                x.StandardResponsibilityAllowanceAmount,
                x.DisplayOrder,
                x.IsActive,
                x.Note))
            .ToListAsync(cancellationToken);

        var mappings = await (
                from mapping in dbContext.PayrollResponsibilityAllowanceGradePositions.AsNoTracking()
                where mapping.Year == year && mapping.Month == month
                join position in dbContext.Positions.AsNoTracking()
                    on mapping.PositionId equals position.Id
                orderby position.Name, position.Code
                select new PayrollResponsibilityAllowanceGradePositionDto(
                    mapping.Id,
                    mapping.Year,
                    mapping.Month,
                    mapping.GradeId,
                    mapping.PositionId,
                    position.Code,
                    position.Name,
                    mapping.IsActive,
                    mapping.Note))
            .ToListAsync(cancellationToken);

        var assignments = await (
                from summary in dbContext.PayrollAllowanceSummaryRecords.AsNoTracking()
                where summary.PayrollYear == year && summary.PayrollMonth == month
                join employee in dbContext.Employees.AsNoTracking()
                    on summary.EmployeeId equals employee.Id
                join assignment in dbContext.PayrollResponsibilityAllowanceEmployeeAssignments.AsNoTracking()
                    on summary.Id equals assignment.PayrollAllowanceSummaryRecordId into assignmentGroup
                from assignment in assignmentGroup.DefaultIfEmpty()
                join position in dbContext.Positions.AsNoTracking()
                    on employee.PositionId equals position.Id into positionGroup
                from position in positionGroup.DefaultIfEmpty()
                join grade in dbContext.PayrollResponsibilityAllowanceGrades.AsNoTracking()
                    on assignment.GradeId equals grade.Id into gradeGroup
                from grade in gradeGroup.DefaultIfEmpty()
                orderby employee.EmployeeCode, employee.LastName, employee.FirstName
                select new PayrollResponsibilityAllowanceEmployeeAssignmentDto(
                    assignment == null ? summary.Id : assignment.Id,
                    year,
                    month,
                    summary.EmployeeId,
                    employee.EmployeeCode,
                    BuildEmployeeName(employee.LastName, employee.FirstName),
                    employee.PositionId,
                    position == null ? string.Empty : position.Name,
                    assignment == null ? null : assignment.GradeId,
                    grade == null ? null : grade.Code,
                    grade == null ? string.Empty : grade.Name,
                    grade == null ? 0m : grade.StandardResponsibilityAllowanceAmount,
                    assignment != null && assignment.IsAssignGradeFromPosition,
                    assignment == null
                        ? string.Empty
                        : assignment.IsAssignGradeFromPosition ? PositionDefaultSourceKey : EmployeeAssignmentSourceKey,
                    assignment == null ? null : assignment.Note))
            .ToListAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceGradeConfigDto(
            year,
            month,
            grades,
            mappings,
            assignments);
    }

    /// <summary>
    /// Tạo hoặc cập nhật bậc trách nhiệm. Mã/tên được chuẩn hóa, tiền được làm
    /// tròn hai chữ số; một bản ghi đang sửa không được phép chuyển sang kỳ khác.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceGradeDto> SaveGradeAsync(
        SavePayrollResponsibilityAllowanceGradeRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        var normalizedCode = NormalizeRequired(request.Code, "Mã bậc").ToUpperInvariant();
        var normalizedName = NormalizeRequired(request.Name, "Tên bậc");
        var normalizedNote = NormalizeOptional(request.Note);

        if (request.StandardResponsibilityAllowanceAmount < 0)
        {
            throw new InvalidOperationException("Số tiền chuẩn phải là số không âm.");
        }

        if (request.DisplayOrder < 0)
        {
            throw new InvalidOperationException("Thứ tự hiển thị phải là số không âm.");
        }

        var now = GetDatabaseNow();
        PayrollResponsibilityAllowanceGradeRow? row;
        // Có ID là sửa đúng record hiện hữu; không có ID là upsert theo mã bậc trong kỳ.
        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            row = await dbContext.PayrollResponsibilityAllowanceGrades
                .SingleOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy cấp bậc trách nhiệm cần cập nhật. Hãy tải lại dữ liệu kỳ hiện tại.");

            if (row.Year != request.Year || row.Month != request.Month)
            {
                throw new InvalidOperationException("Không thể chuyển cấp bậc trách nhiệm đang sửa sang kỳ khác. Hãy đóng popup và tải lại dữ liệu.");
            }
        }
        else
        {
            row = await dbContext.PayrollResponsibilityAllowanceGrades
                .SingleOrDefaultAsync(
                    x => x.Year == request.Year && x.Month == request.Month && x.Code == normalizedCode,
                    cancellationToken);
        }

        // Chỉ tạo entity mới khi kỳ hiện tại chưa có bậc cùng mã.
        if (row is null)
        {
            row = new PayrollResponsibilityAllowanceGradeRow
            {
                Id = request.Id.GetValueOrDefault(Guid.NewGuid()),
                CreatedAtUtc = now
            };
            dbContext.PayrollResponsibilityAllowanceGrades.Add(row);
        }

        row.Year = request.Year;
        row.Month = request.Month;
        row.Code = normalizedCode;
        row.Name = normalizedName;
        row.StandardResponsibilityAllowanceAmount = decimal.Round(request.StandardResponsibilityAllowanceAmount, 2, MidpointRounding.AwayFromZero);
        row.DisplayOrder = request.DisplayOrder;
        row.IsActive = request.IsActive;
        row.Note = normalizedNote;
        row.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceGradeDto(
            row.Id,
            row.Year,
            row.Month,
            row.Code,
            row.Name,
            row.StandardResponsibilityAllowanceAmount,
            row.DisplayOrder,
            row.IsActive,
            row.Note);
    }

    /// <summary>
    /// Tạo/cập nhật ánh xạ một chức vụ với một bậc trong đúng kỳ. Mỗi chức vụ chỉ
    /// có một mapping trong kỳ vì truy vấn tìm bản ghi theo PositionId.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceGradePositionDto> SaveMappingAsync(
        SavePayrollResponsibilityAllowanceGradePositionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(request.Year, request.Month);

        // Cả bậc và chức vụ phải tồn tại trước khi thiết lập khóa ngoại mapping.
        var grade = await dbContext.PayrollResponsibilityAllowanceGrades
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == request.GradeId && x.Year == request.Year && x.Month == request.Month,
                cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy cấp bậc trách nhiệm của kỳ đã chọn.");

        var position = await dbContext.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.PositionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy chức vụ đã chọn.");

        var now = GetDatabaseNow();
        PayrollResponsibilityAllowanceGradePositionRow? row;
        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            row = await dbContext.PayrollResponsibilityAllowanceGradePositions
                .SingleOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken)
                ?? throw new InvalidOperationException("Không tìm thấy mapping chức vụ cần cập nhật. Hãy tải lại dữ liệu kỳ hiện tại.");

            if (row.Year != request.Year || row.Month != request.Month)
            {
                throw new InvalidOperationException("Không thể chuyển mapping chức vụ đang sửa sang kỳ khác. Hãy đóng popup và tải lại dữ liệu.");
            }
        }
        else
        {
            row = await dbContext.PayrollResponsibilityAllowanceGradePositions
                .SingleOrDefaultAsync(
                    x => x.Year == request.Year && x.Month == request.Month && x.PositionId == request.PositionId,
                    cancellationToken);
        }

        if (row is null)
        {
            row = new PayrollResponsibilityAllowanceGradePositionRow
            {
                Id = request.Id.GetValueOrDefault(Guid.NewGuid()),
                CreatedAtUtc = now
            };
            dbContext.PayrollResponsibilityAllowanceGradePositions.Add(row);
        }

        row.Year = request.Year;
        row.Month = request.Month;
        row.GradeId = grade.Id;
        row.PositionId = position.Id;
        row.IsActive = request.IsActive;
        row.Note = NormalizeOptional(request.Note);
        row.UpdatedAtUtc = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceGradePositionDto(
            row.Id,
            row.Year,
            row.Month,
            row.GradeId,
            row.PositionId,
            position.Code,
            position.Name,
            row.IsActive,
            row.Note);
    }

    /// <summary>
    /// Ngừng hiệu lực mapping bằng soft deactivate, không xóa lịch sử cấu hình
    /// đã dùng để tính hoặc kiểm tra các kỳ trước đó.
    /// </summary>
    public async Task<PayrollResponsibilityAllowanceGradePositionDto> DeactivateMappingAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var row = await dbContext.PayrollResponsibilityAllowanceGradePositions
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mapping chức vụ cần ngừng dùng.");

        var position = await dbContext.Positions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == row.PositionId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy chức vụ của mapping đã chọn.");

        // Soft deactivate giữ nguyên ID, bậc và ghi chú để truy vết cấu hình đã dùng.
        row.IsActive = false;
        row.UpdatedAtUtc = GetDatabaseNow();

        await dbContext.SaveChangesAsync(cancellationToken);

        return new PayrollResponsibilityAllowanceGradePositionDto(
            row.Id,
            row.Year,
            row.Month,
            row.GradeId,
            row.PositionId,
            position.Code,
            position.Name,
            row.IsActive,
            row.Note);
    }

    public async Task<PayrollResponsibilityAllowanceConfigCopyResult> CopyFromPreviousMonthAsync(
        int year,
        int month,
        bool copyMappings,
        CancellationToken cancellationToken = default)
    {
        ValidatePeriod(year, month);
        var previous = new ResponsibilityAllowancePeriod(year, month).GetPreviousPeriod();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = GetDatabaseNow();
        var created = 0;
        var skipped = 0;

        if (!copyMappings)
        {
            var source = await dbContext.PayrollResponsibilityAllowanceGrades
                .AsNoTracking()
                .Where(x => x.Year == previous.Year && x.Month == previous.Month)
                .ToListAsync(cancellationToken);
            var existingCodes = (await dbContext.PayrollResponsibilityAllowanceGrades
                    .Where(x => x.Year == year && x.Month == month)
                    .Select(x => x.Code)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var grade in source)
            {
                if (!existingCodes.Add(grade.Code))
                {
                    skipped++;
                    continue;
                }

                dbContext.PayrollResponsibilityAllowanceGrades.Add(new PayrollResponsibilityAllowanceGradeRow
                {
                    Id = Guid.NewGuid(),
                    Year = year,
                    Month = month,
                    Code = grade.Code,
                    Name = grade.Name,
                    StandardResponsibilityAllowanceAmount = grade.StandardResponsibilityAllowanceAmount,
                    DisplayOrder = grade.DisplayOrder,
                    IsActive = grade.IsActive,
                    Note = grade.Note,
                    CreatedAtUtc = now
                });
                created++;
            }
        }
        else
        {
            var targetGrades = await dbContext.PayrollResponsibilityAllowanceGrades
                .Where(x => x.Year == year && x.Month == month)
                .ToListAsync(cancellationToken);
            var targetGradeByCode = targetGrades.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
            var existingPositions = (await dbContext.PayrollResponsibilityAllowanceGradePositions
                    .Where(x => x.Year == year && x.Month == month)
                    .Select(x => x.PositionId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();
            var sourceMappings = await (
                    from mapping in dbContext.PayrollResponsibilityAllowanceGradePositions.AsNoTracking()
                    join grade in dbContext.PayrollResponsibilityAllowanceGrades.AsNoTracking()
                        on mapping.GradeId equals grade.Id
                    where mapping.Year == previous.Year && mapping.Month == previous.Month
                    select new { mapping, GradeCode = grade.Code })
                .ToListAsync(cancellationToken);

            foreach (var sourceMapping in sourceMappings)
            {
                if (!targetGradeByCode.TryGetValue(sourceMapping.GradeCode, out var targetGrade)
                    || !existingPositions.Add(sourceMapping.mapping.PositionId))
                {
                    skipped++;
                    continue;
                }

                dbContext.PayrollResponsibilityAllowanceGradePositions.Add(new PayrollResponsibilityAllowanceGradePositionRow
                {
                    Id = Guid.NewGuid(),
                    Year = year,
                    Month = month,
                    GradeId = targetGrade.Id,
                    PositionId = sourceMapping.mapping.PositionId,
                    IsActive = sourceMapping.mapping.IsActive,
                    Note = sourceMapping.mapping.Note,
                    CreatedAtUtc = now
                });
                created++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new PayrollResponsibilityAllowanceConfigCopyResult(year, month, copyMappings, created, skipped);
    }

    #endregion
}
