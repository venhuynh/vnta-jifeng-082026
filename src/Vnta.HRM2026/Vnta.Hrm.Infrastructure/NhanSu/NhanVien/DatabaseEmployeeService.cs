using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vnta.Hrm.Application.Common;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.NhanSu.NhanVien;

public sealed class DatabaseEmployeeService(ApplicationDbContext dbContext)
    : IEmployeeService,
        INhanVienListReadService,
        INhanVienSummaryReadService,
        INhanVienCreateService,
        INhanVienEditService,
        INhanVienDeleteService,
        INhanVienStatusService,
        INhanVienExportReadService
{
    private const int MaxSearchResultLimit = 10000;
    private const int DefaultPagedSearchResultLimit = 50;
    private const int MaxPagedSearchResultLimit = 200;
    private const string EmployeeCodeUniqueIndexName = "ux_employees_employee_code_active";

    public Task<IReadOnlyList<EmployeeListItemDto>> GetAsync(CancellationToken cancellationToken = default) =>
        SearchAsync(new EmployeeFilter(null), cancellationToken);

    public async Task<IReadOnlyList<EmployeeListItemDto>> SearchAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var take = Math.Clamp(filter.Take, 1, MaxSearchResultLimit);
        var normalizedSearchText = Normalize(filter.SearchText);
        var normalizedStatuses = NormalizeStatuses(filter.Statuses);

        var query = BuildFilteredEmployeeQuery(normalizedSearchText, normalizedStatuses);

        return await query
            .OrderBy(x => x.Employee.EmployeeCode)
            .ThenBy(x => x.Employee.LastName)
            .ThenBy(x => x.Employee.FirstName)
            .ThenBy(x => x.Employee.Id)
            .Take(take)
            .Select(x => new EmployeeListItemDto(
                x.Employee.Id,
                x.Employee.EmployeeCode,
                x.Employee.FirstName,
                x.Employee.LastName,
                x.Employee.Email,
                x.Employee.PhoneNumber,
                AvatarImageSourceHelper.NormalizeSource(x.Employee.Avatar),
                x.Employee.HireDate,
                x.Employee.DepartmentId,
                x.Department == null ? null : x.Department.Code,
                x.Department == null ? null : BuildDepartmentName(x.Department),
                x.Department == null ? null : BuildDepartmentPath(x.Department),
                x.Employee.PositionId,
                x.Position == null ? null : x.Position.Code,
                x.Position == null ? null : x.Position.Name,
                x.Employee.Status,
                x.Employee.SeniorityStartDate,
                x.Employee.ResignedDate,
                x.Employee.CreatedAtUtc,
                x.Employee.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<NhanVienListPageDto> SearchPageAsync(
        NhanVienListQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var skip = Math.Max(0, query.Skip);
        var take = query.Take <= 0
            ? DefaultPagedSearchResultLimit
            : Math.Min(query.Take, MaxPagedSearchResultLimit);
        var normalizedSearchText = Normalize(query.SearchText);
        var normalizedStatuses = NormalizeStatuses(query.Statuses);

        var filteredQuery = BuildFilteredEmployeeQuery(normalizedSearchText, normalizedStatuses);

        var totalCount = await filteredQuery.CountAsync(cancellationToken);
        if (totalCount == 0 || skip >= totalCount)
        {
            return new NhanVienListPageDto([], totalCount);
        }

        var rows = await filteredQuery
            .OrderBy(x => x.Employee.EmployeeCode)
            .ThenBy(x => x.Employee.LastName)
            .ThenBy(x => x.Employee.FirstName)
            .ThenBy(x => x.Employee.Id)
            .Skip(skip)
            .Take(take)
            .Select(x => new EmployeeListItemDto(
                x.Employee.Id,
                x.Employee.EmployeeCode,
                x.Employee.FirstName,
                x.Employee.LastName,
                x.Employee.Email,
                x.Employee.PhoneNumber,
                AvatarImageSourceHelper.NormalizeSource(x.Employee.Avatar),
                x.Employee.HireDate,
                x.Employee.DepartmentId,
                x.Department == null ? null : x.Department.Code,
                x.Department == null ? null : BuildDepartmentName(x.Department),
                x.Department == null ? null : BuildDepartmentPath(x.Department),
                x.Employee.PositionId,
                x.Position == null ? null : x.Position.Code,
                x.Position == null ? null : x.Position.Name,
                x.Employee.Status,
                x.Employee.SeniorityStartDate,
                x.Employee.ResignedDate,
                x.Employee.CreatedAtUtc,
                x.Employee.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return new NhanVienListPageDto(rows, totalCount);
    }

    public async Task<IReadOnlyList<EmployeeListItemDto>> ExportAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await BuildFilteredEmployeeQuery(null, null)
            .OrderBy(x => x.Employee.EmployeeCode)
            .ThenBy(x => x.Employee.LastName)
            .ThenBy(x => x.Employee.FirstName)
            .ThenBy(x => x.Employee.Id)
            .Select(x => new EmployeeListItemDto(
                x.Employee.Id,
                x.Employee.EmployeeCode,
                x.Employee.FirstName,
                x.Employee.LastName,
                x.Employee.Email,
                x.Employee.PhoneNumber,
                AvatarImageSourceHelper.NormalizeSource(x.Employee.Avatar),
                x.Employee.HireDate,
                x.Employee.DepartmentId,
                x.Department == null ? null : x.Department.Code,
                x.Department == null ? null : BuildDepartmentName(x.Department),
                x.Department == null ? null : BuildDepartmentPath(x.Department),
                x.Employee.PositionId,
                x.Position == null ? null : x.Position.Code,
                x.Position == null ? null : x.Position.Name,
                x.Employee.Status,
                x.Employee.SeniorityStartDate,
                x.Employee.ResignedDate,
                x.Employee.CreatedAtUtc,
                x.Employee.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployeeSummaryDto> GetSummaryAsync(
        EmployeeFilter filter,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedSearchText = Normalize(filter.SearchText);
        var normalizedStatuses = NormalizeStatuses(filter.Statuses);

        var query = BuildFilteredEmployeeQuery(normalizedSearchText, normalizedStatuses);

        var summary = await query
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalCount = group.Count(),
                WorkingCount = group.Count(x =>
                    x.Employee.Status == EmployeeStatusCatalog.Probation
                    || x.Employee.Status == EmployeeStatusCatalog.Official),
                ProbationCount = group.Count(x => x.Employee.Status == EmployeeStatusCatalog.Probation),
                OfficialCount = group.Count(x => x.Employee.Status == EmployeeStatusCatalog.Official),
                ResignedCount = group.Count(x => x.Employee.Status == EmployeeStatusCatalog.Resigned)
            })
            .SingleOrDefaultAsync(cancellationToken);

        return summary is null
            ? new EmployeeSummaryDto(0, 0, 0, 0, 0)
            : new EmployeeSummaryDto(
                summary.TotalCount,
                summary.WorkingCount,
                summary.ProbationCount,
                summary.OfficialCount,
                summary.ResignedCount);
    }

    public async Task<EmployeeListItemDto> CreateAsync(
        CreateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedCode = EmployeeCodeNormalizer.Normalize(request.EmployeeCode) ?? string.Empty;
        var normalizedFullName = Normalize(request.FullName) ?? string.Empty;
        var normalizedEmail = Normalize(request.Email);
        var normalizedPhoneNumber = Normalize(request.PhoneNumber);

        if (normalizedCode.Length != 5)
        {
            throw new InvalidOperationException("Mã nhân viên phải có đúng 5 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            throw new InvalidOperationException("Họ tên không được để trống.");
        }

        if (request.DepartmentId == Guid.Empty)
        {
            throw new InvalidOperationException("Phòng ban không được để trống.");
        }

        if (request.PositionId == Guid.Empty)
        {
            throw new InvalidOperationException("Chức vụ không được để trống.");
        }

        if (!IsSupportedStatus(request.Status))
        {
            throw new InvalidOperationException("Tình trạng nhân viên không hợp lệ.");
        }

        var duplicateCode = await dbContext.Employees
            .AsNoTracking()
            .AnyAsync(
                employee => !employee.IsDeleted
                    && employee.EmployeeCode.ToLower() == normalizedCode.ToLower(),
                cancellationToken);
        if (duplicateCode)
        {
            throw new InvalidOperationException("Mã nhân viên đã tồn tại.");
        }

        var departmentExists = await dbContext.Departments
            .AsNoTracking()
            .AnyAsync(department => department.Id == request.DepartmentId, cancellationToken);
        if (!departmentExists)
        {
            throw new InvalidOperationException("Phòng ban đã chọn không tồn tại.");
        }

        var positionExists = await dbContext.Positions
            .AsNoTracking()
            .AnyAsync(position => position.Id == request.PositionId, cancellationToken);
        if (!positionExists)
        {
            throw new InvalidOperationException("Chức vụ đã chọn không tồn tại.");
        }

        var (lastName, firstName) = SplitFullName(normalizedFullName);
        var utcNow = ToDatabaseTimestamp(DateTime.UtcNow);
        var hireDate = request.HireDate.HasValue
            ? ToDatabaseTimestamp(request.HireDate.Value)
            : utcNow.Date;
        var row = new AttendanceGatewayEmployeeRow
        {
            Id = Guid.NewGuid(),
            EmployeeCode = normalizedCode,
            FirstName = firstName,
            LastName = lastName,
            Email = normalizedEmail,
            PhoneNumber = normalizedPhoneNumber,
            HireDate = hireDate,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            Status = request.Status,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow,
            IsDeleted = false
        };

        dbContext.Employees.Add(row);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateEmployeeCodeViolation(ex))
        {
            throw new InvalidOperationException("Mã nhân viên đã tồn tại.", ex);
        }

        return await GetByIdAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại nhân viên vừa tạo.");
    }

    public async Task<EmployeeListItemDto> UpdateAsync(
        UpdateEmployeeRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Nhân viên cần điều chỉnh không hợp lệ.");
        }

        var normalizedCode = EmployeeCodeNormalizer.Normalize(request.EmployeeCode) ?? string.Empty;
        var normalizedFullName = Normalize(request.FullName) ?? string.Empty;

        if (normalizedCode.Length != 5)
        {
            throw new InvalidOperationException("Mã nhân viên phải có đúng 5 ký tự.");
        }

        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            throw new InvalidOperationException("Họ tên không được để trống.");
        }

        if (request.DepartmentId == Guid.Empty)
        {
            throw new InvalidOperationException("Phòng ban không được để trống.");
        }

        if (request.PositionId == Guid.Empty)
        {
            throw new InvalidOperationException("Chức vụ không được để trống.");
        }

        if (!IsSupportedStatus(request.Status))
        {
            throw new InvalidOperationException("Tình trạng nhân viên không hợp lệ.");
        }

        var row = await dbContext.Employees
            .SingleOrDefaultAsync(
                employee => employee.Id == request.Id && !employee.IsDeleted,
                cancellationToken);
        if (row is null)
        {
            throw new InvalidOperationException("Không tìm thấy nhân viên cần điều chỉnh.");
        }

        if (!string.Equals(normalizedCode, row.EmployeeCode, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Không được phép thay đổi mã nhân viên.");
        }

        if (!request.OriginalUpdatedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "Thiếu mốc đối soát cập nhật nhân viên. Vui lòng tải lại danh sách và thử lại.");
        }

        var originalUpdatedAtUtc = ToDatabaseTimestamp(request.OriginalUpdatedAtUtc.Value);
        var currentUpdatedAtUtc = row.UpdatedAtUtc ?? row.CreatedAtUtc;
        if (currentUpdatedAtUtc != originalUpdatedAtUtc)
        {
            throw new InvalidOperationException(
                "Hồ sơ nhân viên đã được cập nhật ở phiên khác. Vui lòng tải lại danh sách trước khi lưu tiếp.");
        }

        var departmentExists = await dbContext.Departments
            .AsNoTracking()
            .AnyAsync(department => department.Id == request.DepartmentId, cancellationToken);
        if (!departmentExists)
        {
            throw new InvalidOperationException("Phòng ban đã chọn không tồn tại.");
        }

        var positionExists = await dbContext.Positions
            .AsNoTracking()
            .AnyAsync(position => position.Id == request.PositionId, cancellationToken);
        if (!positionExists)
        {
            throw new InvalidOperationException("Chức vụ đã chọn không tồn tại.");
        }

        var (lastName, firstName) = SplitFullName(normalizedFullName);
        var utcNow = ToDatabaseTimestamp(DateTime.UtcNow);
        var hireDate = request.HireDate.HasValue
            ? ToDatabaseTimestamp(request.HireDate.Value)
            : row.HireDate;

        row.LastName = lastName;
        row.FirstName = firstName;
        row.HireDate = hireDate;
        row.DepartmentId = request.DepartmentId;
        row.PositionId = request.PositionId;
        row.Status = request.Status;
        if (request.UpdateEmploymentDates)
        {
            ApplyEmploymentDates(
                row,
                request.Status,
                request.SeniorityStartDate,
                request.ResignedDate,
                hireDate.Date);
        }
        row.UpdatedAtUtc = utcNow;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateEmployeeCodeViolation(ex))
        {
            throw new InvalidOperationException("Mã nhân viên đã tồn tại.", ex);
        }

        return await GetByIdAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại nhân viên vừa điều chỉnh.");
    }

    public async Task DeleteAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return;
        }

        var deletedAtUtc = ToDatabaseTimestamp(DateTime.UtcNow);
        await dbContext.Employees
            .Where(employee => normalizedIds.Contains(employee.Id) && !employee.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(employee => employee.IsDeleted, true)
                    .SetProperty(employee => employee.DeletedAtUtc, deletedAtUtc)
                    .SetProperty(employee => employee.UpdatedAtUtc, deletedAtUtc),
                cancellationToken);
    }

    public async Task<EmployeeListItemDto> ChangeStatusAsync(
        ChangeEmployeeStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Nhân viên cần cập nhật tình trạng không hợp lệ.");
        }

        if (!IsSupportedStatus(request.Status))
        {
            throw new InvalidOperationException("Tình trạng nhân viên không hợp lệ.");
        }

        if (!request.OriginalUpdatedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "Thiếu mốc đối soát cập nhật nhân viên. Vui lòng tải lại danh sách và thử lại.");
        }

        var row = await dbContext.Employees.SingleOrDefaultAsync(
            employee => employee.Id == request.Id && !employee.IsDeleted,
            cancellationToken);
        if (row is null)
        {
            throw new InvalidOperationException("Không tìm thấy nhân viên cần cập nhật tình trạng.");
        }

        EnsureUnchanged(row, request.OriginalUpdatedAtUtc.Value);

        ApplyEmploymentDates(
            row,
            request.Status,
            request.SeniorityStartDate,
            request.ResignedDate,
            row.HireDate.Date);

        row.Status = request.Status;
        row.UpdatedAtUtc = ToDatabaseTimestamp(DateTime.UtcNow);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(row.Id, cancellationToken)
            ?? throw new InvalidOperationException("Không thể tải lại nhân viên vừa cập nhật tình trạng.");
    }

    // Cùng predicate được dùng cho lookup legacy, page của màn Nhân viên và summary để count/list không lệch tập dữ liệu.
    private IQueryable<EmployeeQueryRow> BuildFilteredEmployeeQuery(
        string? normalizedSearchText,
        int[]? normalizedStatuses)
    {
        IQueryable<EmployeeQueryRow> query =
            from employee in dbContext.Employees.AsNoTracking()
            where !employee.IsDeleted
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new EmployeeQueryRow
            {
                Employee = employee,
                Department = department,
                Position = position
            };

        if (!string.IsNullOrWhiteSpace(normalizedSearchText))
        {
            var searchPattern = $"%{normalizedSearchText}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Employee.EmployeeCode, searchPattern)
                || EF.Functions.ILike(x.Employee.FirstName, searchPattern)
                || EF.Functions.ILike(x.Employee.LastName, searchPattern)
                || (x.Employee.Email != null && EF.Functions.ILike(x.Employee.Email, searchPattern))
                || (x.Employee.PhoneNumber != null && EF.Functions.ILike(x.Employee.PhoneNumber, searchPattern))
                || (x.Department != null && x.Department.CenterName != null && EF.Functions.ILike(x.Department.CenterName, searchPattern))
                || (x.Department != null && x.Department.DepartmentOrWorkshopName != null && EF.Functions.ILike(x.Department.DepartmentOrWorkshopName, searchPattern))
                || (x.Department != null && x.Department.TeamName != null && EF.Functions.ILike(x.Department.TeamName, searchPattern))
                || (x.Department != null && x.Department.GroupName != null && EF.Functions.ILike(x.Department.GroupName, searchPattern))
                || (x.Position != null && x.Position.Name != null && EF.Functions.ILike(x.Position.Name, searchPattern)));
        }

        if (normalizedStatuses is { Length: > 0 })
        {
            query = query.Where(x => normalizedStatuses.Contains(x.Employee.Status));
        }

        return query;
    }

    private static string BuildDepartmentName(AttendanceDepartmentRow department) =>
        Normalize(department.GroupName)
        ?? Normalize(department.TeamName)
        ?? Normalize(department.DepartmentOrWorkshopName)
        ?? string.Empty;

    private static string BuildDepartmentPath(AttendanceDepartmentRow department) =>
        string.Join(
            " / ",
            new[]
            {
                Normalize(department.CenterName),
                Normalize(department.DepartmentOrWorkshopName),
                Normalize(department.TeamName),
                Normalize(department.GroupName)
            }.Where(static value => !string.IsNullOrWhiteSpace(value)));

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int[]? NormalizeStatuses(IReadOnlyList<int>? statuses)
    {
        if (statuses is not { Count: > 0 })
        {
            return null;
        }

        return statuses
            .Distinct()
            .ToArray();
    }

    private static bool IsSupportedStatus(int status) =>
        status is EmployeeStatusCatalog.Probation
            or EmployeeStatusCatalog.Official
            or EmployeeStatusCatalog.Resigned;

    private static void EnsureUnchanged(AttendanceGatewayEmployeeRow row, DateTime originalUpdatedAtUtc)
    {
        var expected = ToDatabaseTimestamp(originalUpdatedAtUtc);
        var current = row.UpdatedAtUtc ?? row.CreatedAtUtc;
        if (current != expected)
        {
            throw new InvalidOperationException(
                "Hồ sơ nhân viên đã được cập nhật ở phiên khác. Vui lòng tải lại danh sách trước khi lưu tiếp.");
        }
    }

    private static void ApplyEmploymentDates(
        AttendanceGatewayEmployeeRow row,
        int status,
        DateTime? seniorityStartDate,
        DateTime? resignedDate,
        DateTime hireDate)
    {
        var today = DateTime.Today;
        switch (status)
        {
            case EmployeeStatusCatalog.Resigned:
                row.ResignedDate = ValidateBusinessDate(
                    resignedDate,
                    "Ngày nghỉ việc",
                    hireDate,
                    today);
                break;
            case EmployeeStatusCatalog.Official:
                row.SeniorityStartDate = ValidateBusinessDate(
                    seniorityStartDate,
                    "Ngày bắt đầu tính thâm niên",
                    hireDate,
                    today);
                row.ResignedDate = null;
                break;
            case EmployeeStatusCatalog.Probation:
                row.SeniorityStartDate = null;
                row.ResignedDate = null;
                break;
        }
    }

    private static DateTime ValidateBusinessDate(
        DateTime? value,
        string displayName,
        DateTime minimumDate,
        DateTime maximumDate)
    {
        if (!value.HasValue)
        {
            throw new InvalidOperationException($"{displayName} không được để trống.");
        }

        var date = value.Value.Date;
        if (date < minimumDate)
        {
            throw new InvalidOperationException($"{displayName} không được trước ngày vào làm.");
        }

        if (date > maximumDate)
        {
            throw new InvalidOperationException($"{displayName} không được ở tương lai.");
        }

        return ToDatabaseTimestamp(date);
    }

    public async Task<EmployeeListItemDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query =
            from employee in dbContext.Employees.AsNoTracking()
            where employee.Id == id && !employee.IsDeleted
            join department in dbContext.Departments.AsNoTracking()
                on employee.DepartmentId equals department.Id into departmentGroup
            from department in departmentGroup.DefaultIfEmpty()
            join position in dbContext.Positions.AsNoTracking()
                on employee.PositionId equals position.Id into positionGroup
            from position in positionGroup.DefaultIfEmpty()
            select new EmployeeListItemDto(
                employee.Id,
                employee.EmployeeCode,
                employee.FirstName,
                employee.LastName,
                employee.Email,
                employee.PhoneNumber,
                AvatarImageSourceHelper.NormalizeSource(employee.Avatar),
                employee.HireDate,
                employee.DepartmentId,
                department == null ? null : department.Code,
                department == null ? null : BuildDepartmentName(department),
                department == null ? null : BuildDepartmentPath(department),
                employee.PositionId,
                position == null ? null : position.Code,
                position == null ? null : position.Name,
                employee.Status,
                employee.SeniorityStartDate,
                employee.ResignedDate,
                employee.CreatedAtUtc,
                employee.UpdatedAtUtc);

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private static (string LastName, string FirstName) SplitFullName(string fullName)
    {
        var parts = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            0 => (string.Empty, string.Empty),
            1 => (string.Empty, parts[0]),
            _ => (string.Join(" ", parts[..^1]), parts[^1])
        };
    }

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    private static bool IsDuplicateEmployeeCodeViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
        && string.Equals(
            postgresException.ConstraintName,
            EmployeeCodeUniqueIndexName,
            StringComparison.OrdinalIgnoreCase);

    private sealed class EmployeeQueryRow
    {
        public AttendanceGatewayEmployeeRow Employee { get; init; } = default!;

        public AttendanceDepartmentRow? Department { get; init; }

        public AttendanceGatewayPositionRow? Position { get; init; }
    }
}
