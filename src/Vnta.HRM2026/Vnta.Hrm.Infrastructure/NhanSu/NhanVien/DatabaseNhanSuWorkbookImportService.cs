using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.NhanSu.NhanVien;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.NhanSu.NhanVien;

/// <summary>
/// Creates Jifeng employees from the supported fields in the <c>NhanSu</c> worksheet.
/// The implementation is intentionally create-only: an existing active employee code is skipped
/// and never overwritten by an uploaded workbook.
/// </summary>
public sealed class DatabaseNhanSuWorkbookImportService(
    ApplicationDbContext dbContext,
    IAuditScope auditScope,
    IAuditedMutation auditedMutation)
    : INhanSuWorkbookImportService
{
    private const string NhanSuSheetName = "NhanSu";
    private const string SystemActorId = "system";
    private static readonly string[] SourceDateFormats = ["d/M/yyyy"];
    private static readonly CultureInfo VietnameseCulture = CultureInfo.GetCultureInfo("vi-VN");

    public async Task<NhanSuWorkbookImportResultDto> ImportAsync(
        Stream workbookStream,
        DateTime? missingHireDateFallback = null,
        int? activeStatusFallback = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbookStream);
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedFallbackHireDate = NormalizeFallbackHireDate(missingHireDateFallback);
        ValidateActiveStatusFallback(activeStatusFallback);

        // Parse before taking the database lock. The subsequent transactional preflight locks only
        // the short database section that reads existing codes and stages the new rows.
        var sourceRows = await DatabaseNhanSuWorkbookPreviewService
            .ReadSourceRowsAsync(workbookStream, cancellationToken)
            .ConfigureAwait(false);

        var auditCommand = CreateAuditCommand();
        return await auditedMutation.ExecuteAsync(
                auditCommand,
                token => ImportRowsAsync(
                    sourceRows,
                    normalizedFallbackHireDate,
                    activeStatusFallback,
                    token),
                CreateAuditEvent,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<NhanSuWorkbookImportResultDto> ImportRowsAsync(
        IReadOnlyList<DatabaseNhanSuWorkbookPreviewService.SourceEmployeeRow> sourceRows,
        DateTime? missingHireDateFallback,
        int? activeStatusFallback,
        CancellationToken cancellationToken)
    {
        await LockEmployeesForImportIfNeededAsync(cancellationToken).ConfigureAwait(false);

        var existingEmployees = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => !employee.IsDeleted)
            .Select(employee => new ExistingEmployee(employee.Id, employee.EmployeeCode))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var departments = await dbContext.Departments
            .AsNoTracking()
            .Select(department => new DepartmentReference(
                department.Id,
                department.CenterName,
                department.DepartmentOrWorkshopName))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var positions = await dbContext.Positions
            .AsNoTracking()
            .Select(position => new PositionReference(position.Id, position.Name))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingEmployeesByCode = existingEmployees
            .Select(employee => new { Employee = employee, Code = NormalizeEmployeeCode(employee.EmployeeCode) })
            .Where(item => item.Code is not null)
            .GroupBy(item => item.Code!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Employee).ToArray(),
                StringComparer.Ordinal);
        var departmentByPath = departments
            .Select(department => new { Department = department, Key = CreateDepartmentKey(
                department.CenterName,
                department.DepartmentName) })
            .Where(item => item.Key is not null)
            .GroupBy(item => item.Key!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Department).ToArray(),
                StringComparer.Ordinal);
        var positionsByName = positions
            .Select(position => new { Position = position, Name = NormalizeMatchText(position.Name) })
            .Where(item => item.Name is not null)
            .GroupBy(item => item.Name!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Position).ToArray(),
                StringComparer.Ordinal);
        var duplicateSourceCodes = sourceRows
            .Select(row => NormalizeEmployeeCode(row.EmployeeCode))
            .Where(code => code is not null)
            .GroupBy(code => code!, StringComparer.Ordinal)
            .Where(group => group.Skip(1).Any())
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        var issues = new List<NhanSuWorkbookImportIssueDto>();
        var rowsToCreate = new List<AttendanceGatewayEmployeeRow>();
        var skippedExistingCount = 0;
        var fallbackHireDateCount = 0;
        var now = ToDatabaseTimestamp(DateTime.UtcNow);
        foreach (var sourceRow in sourceRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var issueCountBeforeRow = issues.Count;
            var employeeCode = ValidateEmployeeCode(sourceRow, duplicateSourceCodes, issues);
            var fullName = ValidateFullName(sourceRow, issues, out var lastName, out var firstName);
            var department = ResolveDepartment(sourceRow, departmentByPath, issues);
            var position = ResolvePosition(sourceRow, positionsByName, issues);
            var employment = ResolveEmployment(
                sourceRow,
                missingHireDateFallback,
                activeStatusFallback,
                issues);
            var email = ValidateOptionalValue(sourceRow, sourceRow.CompanyEmail, "CompanyEmail", 256, issues);
            var phone = ValidateOptionalValue(sourceRow, sourceRow.Phone, "Phone", 30, issues);

            var hasErrors = issues
                .Skip(issueCountBeforeRow)
                .Any(issue => issue.Severity == NhanSuWorkbookImportIssueSeverity.Error);
            if (hasErrors)
            {
                continue;
            }

            if (employeeCode is null
                || fullName is null
                || department is null
                || position is null
                || employment is null)
            {
                // Defensive invariant: every missing value above should already have a safe issue.
                AddError(issues, sourceRow.SourceRowNumber, "Source", "Dòng nguồn không đủ dữ liệu để import.");
                continue;
            }

            if (existingEmployeesByCode.TryGetValue(employeeCode, out var existingCandidates))
            {
                if (existingCandidates.Length == 1)
                {
                    skippedExistingCount++;
                    continue;
                }

                AddError(
                    issues,
                    sourceRow.SourceRowNumber,
                    "Code",
                    "Có nhiều nhân viên đang hoạt động cùng mã trong hệ thống.");
                continue;
            }

            if (employment.UsedFallbackHireDate)
            {
                fallbackHireDateCount++;
            }

            rowsToCreate.Add(new AttendanceGatewayEmployeeRow
            {
                Id = Guid.NewGuid(),
                EmployeeCode = employeeCode,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                PhoneNumber = phone,
                HireDate = employment.HireDate,
                SeniorityStartDate = employment.SeniorityStartDate,
                ResignedDate = employment.ResignedDate,
                DepartmentId = department.Id,
                PositionId = position.Id,
                Status = employment.Status,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                IsDeleted = false
            });
        }

        if (issues.Any(issue => issue.Severity == NhanSuWorkbookImportIssueSeverity.Error))
        {
            throw new NhanSuWorkbookImportValidationException(issues.ToArray());
        }

        if (rowsToCreate.Count > 0)
        {
            dbContext.Employees.AddRange(rowsToCreate);
        }

        return new NhanSuWorkbookImportResultDto(
            sourceRows.Count,
            rowsToCreate.Count,
            skippedExistingCount,
            sourceRows.Sum(row => row.FormulaErrorCount),
            fallbackHireDateCount,
            issues
                .Where(issue => issue.Severity == NhanSuWorkbookImportIssueSeverity.Warning)
                .ToArray());
    }

    private static string? ValidateEmployeeCode(
        DatabaseNhanSuWorkbookPreviewService.SourceEmployeeRow sourceRow,
        ISet<string> duplicateSourceCodes,
        ICollection<NhanSuWorkbookImportIssueDto> issues)
    {
        var employeeCode = NormalizeEmployeeCode(sourceRow.EmployeeCode);
        if (employeeCode is null)
        {
            AddError(issues, sourceRow.SourceRowNumber, "Code", "Mã nhân viên là bắt buộc.");
            return null;
        }

        if (IsSpreadsheetError(employeeCode))
        {
            AddError(issues, sourceRow.SourceRowNumber, "Code", "Mã nhân viên chứa kết quả công thức không hợp lệ.");
            return null;
        }

        if (employeeCode.Length > 50)
        {
            AddError(issues, sourceRow.SourceRowNumber, "Code", "Mã nhân viên không được vượt quá 50 ký tự.");
        }

        if (duplicateSourceCodes.Contains(employeeCode))
        {
            AddError(issues, sourceRow.SourceRowNumber, "Code", "Mã nhân viên bị lặp trong sheet NhanSu.");
        }

        return employeeCode;
    }

    private static string? ValidateFullName(
        DatabaseNhanSuWorkbookPreviewService.SourceEmployeeRow sourceRow,
        ICollection<NhanSuWorkbookImportIssueDto> issues,
        out string lastName,
        out string firstName)
    {
        lastName = string.Empty;
        firstName = string.Empty;
        var fullName = NormalizeValue(sourceRow.FullName);
        if (fullName is null)
        {
            AddError(issues, sourceRow.SourceRowNumber, "Name", "Họ tên nhân viên là bắt buộc.");
            return null;
        }

        if (IsSpreadsheetError(fullName))
        {
            AddError(issues, sourceRow.SourceRowNumber, "Name", "Họ tên chứa kết quả công thức không hợp lệ.");
            return null;
        }

        var nameParts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (nameParts.Length == 0)
        {
            AddError(issues, sourceRow.SourceRowNumber, "Name", "Họ tên nhân viên là bắt buộc.");
            return null;
        }

        firstName = nameParts[^1];
        lastName = nameParts.Length == 1 ? string.Empty : string.Join(" ", nameParts[..^1]);
        if (firstName.Length > 100)
        {
            AddError(issues, sourceRow.SourceRowNumber, "Name", "Tên nhân viên không được vượt quá 100 ký tự.");
        }

        if (lastName.Length > 150)
        {
            AddError(issues, sourceRow.SourceRowNumber, "Name", "Họ và tên đệm không được vượt quá 150 ký tự.");
        }

        return fullName;
    }

    private static DepartmentReference? ResolveDepartment(
        DatabaseNhanSuWorkbookPreviewService.SourceEmployeeRow sourceRow,
        IReadOnlyDictionary<string, DepartmentReference[]> departmentsByPath,
        ICollection<NhanSuWorkbookImportIssueDto> issues)
    {
        var departmentLevel1 = NormalizeValue(sourceRow.DepartmentLevel1);
        if (departmentLevel1 is null || IsSpreadsheetError(departmentLevel1))
        {
            AddError(issues, sourceRow.SourceRowNumber, "DepartmentLevel1", "Phòng ban cấp 1 không hợp lệ.");
            return null;
        }

        var departmentLevel2 = NormalizeValue(sourceRow.DepartmentLevel2);
        if (departmentLevel2 is not null && IsSpreadsheetError(departmentLevel2))
        {
            AddError(issues, sourceRow.SourceRowNumber, "DepartmentLevel2", "Phòng ban cấp 2 chứa kết quả công thức không hợp lệ.");
            return null;
        }

        var departmentName = departmentLevel2 ?? departmentLevel1;
        var key = CreateDepartmentKey(departmentLevel1, departmentName);
        if (key is null || !departmentsByPath.TryGetValue(key, out var candidates))
        {
            AddError(issues, sourceRow.SourceRowNumber, "Department", "Không tìm thấy phòng ban tương ứng đã seed.");
            return null;
        }

        if (candidates.Length != 1)
        {
            AddError(issues, sourceRow.SourceRowNumber, "Department", "Phòng ban nguồn map tới nhiều bản ghi hệ thống.");
            return null;
        }

        return candidates[0];
    }

    private static PositionReference? ResolvePosition(
        DatabaseNhanSuWorkbookPreviewService.SourceEmployeeRow sourceRow,
        IReadOnlyDictionary<string, PositionReference[]> positionsByName,
        ICollection<NhanSuWorkbookImportIssueDto> issues)
    {
        var title = NormalizeMatchText(sourceRow.Title);
        if (title is null || IsSpreadsheetError(title))
        {
            AddError(issues, sourceRow.SourceRowNumber, "TitleId", "Tên chức danh không hợp lệ.");
            return null;
        }

        if (!positionsByName.TryGetValue(title, out var candidates))
        {
            AddError(issues, sourceRow.SourceRowNumber, "TitleId", "Không tìm thấy chức danh tương ứng đã seed.");
            return null;
        }

        if (candidates.Length != 1)
        {
            AddError(issues, sourceRow.SourceRowNumber, "TitleId", "Chức danh nguồn map tới nhiều bản ghi hệ thống.");
            return null;
        }

        return candidates[0];
    }

    private static EmploymentValues? ResolveEmployment(
        DatabaseNhanSuWorkbookPreviewService.SourceEmployeeRow sourceRow,
        DateTime? missingHireDateFallback,
        int? activeStatusFallback,
        ICollection<NhanSuWorkbookImportIssueDto> issues)
    {
        var startWorkDate = ParseOptionalDate(sourceRow, sourceRow.StartWorkDate, "StartWorkDate", issues);
        var probationDate = ParseOptionalDate(sourceRow, sourceRow.ProbationDate, "ProbationDate", issues);
        var officialStartDate = ParseOptionalDate(sourceRow, sourceRow.OfficialStartDate, "OfficialStartDate", issues);
        var leaveJobDate = ParseOptionalDate(sourceRow, sourceRow.LeaveJobDate, "LeaveJobDate", issues);
        var contractEffectiveDate = ParseOptionalDate(sourceRow, sourceRow.ContractEffectiveDate, "ContractEffectiveDate", issues);

        var sourceStatus = NormalizeMatchText(sourceRow.WorkStatus);
        if (sourceStatus is null || IsSpreadsheetError(sourceStatus))
        {
            AddError(issues, sourceRow.SourceRowNumber, "WorkStatus", "Tình trạng công tác không hợp lệ.");
            return null;
        }

        var hireDate = startWorkDate
            ?? probationDate
            ?? officialStartDate
            ?? contractEffectiveDate
            ?? missingHireDateFallback;
        if (!hireDate.HasValue)
        {
            AddError(issues, sourceRow.SourceRowNumber, "HireDate", "Không xác định được ngày vào làm từ dữ liệu nguồn.");
        }

        var usedFallbackHireDate = !startWorkDate.HasValue
            && !probationDate.HasValue
            && !officialStartDate.HasValue
            && !contractEffectiveDate.HasValue;
        if (!startWorkDate.HasValue && contractEffectiveDate.HasValue)
        {
            AddWarning(
                issues,
                sourceRow.SourceRowNumber,
                "HireDate",
                "Đã dùng ngày hiệu lực hợp đồng làm ngày vào làm vì cột StartWorkDate trống.");
        }

        if (usedFallbackHireDate && missingHireDateFallback.HasValue)
        {
            AddWarning(
                issues,
                sourceRow.SourceRowNumber,
                "HireDate",
                "Đã dùng ngày vào làm xác nhận khi nguồn không có ngày làm việc.");
        }

        var today = DateTime.Today;
        if (hireDate.HasValue && hireDate.Value.Date > today)
        {
            AddError(issues, sourceRow.SourceRowNumber, "HireDate", "Ngày vào làm không được ở tương lai.");
        }

        var normalizedHireDate = hireDate.HasValue
            ? ToDatabaseTimestamp(hireDate.Value)
            : (DateTime?)null;
        if (sourceStatus == NormalizeMatchText("Đang làm việc"))
        {
            if (leaveJobDate.HasValue)
            {
                AddWarning(
                    issues,
                    sourceRow.SourceRowNumber,
                    "LeaveJobDate",
                    "Nguồn có ngày nghỉ việc nhưng tình trạng công tác vẫn đang làm việc; ngày nghỉ việc không được import.");
            }

            if (officialStartDate.HasValue
                && normalizedHireDate.HasValue
                && officialStartDate.Value.Date <= today)
            {
                if (officialStartDate.Value.Date < normalizedHireDate.Value.Date)
                {
                    AddError(issues, sourceRow.SourceRowNumber, "OfficialStartDate", "Ngày lên chính thức không được trước ngày vào làm.");
                    return null;
                }

                return new EmploymentValues(
                    EmployeeStatusCatalog.Official,
                    normalizedHireDate.Value,
                    ToDatabaseTimestamp(officialStartDate.Value),
                    null,
                    usedFallbackHireDate);
            }

            if (officialStartDate.HasValue && officialStartDate.Value.Date > today)
            {
                AddWarning(
                    issues,
                    sourceRow.SourceRowNumber,
                    "OfficialStartDate",
                    "Ngày lên chính thức ở tương lai; nhân viên được import ở trạng thái thử việc.");
            }

            if (probationDate.HasValue && normalizedHireDate.HasValue)
            {
                if (probationDate.Value.Date < normalizedHireDate.Value.Date)
                {
                    AddError(issues, sourceRow.SourceRowNumber, "ProbationDate", "Ngày thử việc không được trước ngày vào làm.");
                    return null;
                }

                return new EmploymentValues(
                    EmployeeStatusCatalog.Probation,
                    normalizedHireDate.Value,
                    null,
                    null,
                    usedFallbackHireDate);
            }

            if (activeStatusFallback is EmployeeStatusCatalog.Official or EmployeeStatusCatalog.Probation
                && normalizedHireDate.HasValue)
            {
                AddWarning(
                    issues,
                    sourceRow.SourceRowNumber,
                    "Status",
                    "Đã dùng trạng thái làm việc được xác nhận vì nguồn không có ngày thử việc hoặc ngày lên chính thức.");
                return new EmploymentValues(
                    activeStatusFallback.Value,
                    normalizedHireDate.Value,
                    null,
                    null,
                    usedFallbackHireDate);
            }

            AddError(
                issues,
                sourceRow.SourceRowNumber,
                "Status",
                "Cần xác nhận tình trạng thử việc hoặc chính thức vì nguồn không có ngày mốc tương ứng.");
            return null;
        }

        if (sourceStatus is "THỬ VIỆC" or "ĐANG THỬ VIỆC")
        {
            if (!normalizedHireDate.HasValue)
            {
                return null;
            }

            return new EmploymentValues(
                EmployeeStatusCatalog.Probation,
                normalizedHireDate.Value,
                null,
                null,
                usedFallbackHireDate);
        }

        if (sourceStatus is "NGHỈ VIỆC" or "ĐÃ NGHỈ VIỆC")
        {
            if (!normalizedHireDate.HasValue)
            {
                return null;
            }

            if (!leaveJobDate.HasValue)
            {
                AddError(issues, sourceRow.SourceRowNumber, "LeaveJobDate", "Nhân viên nghỉ việc cần có ngày nghỉ việc.");
                return null;
            }

            if (leaveJobDate.Value.Date < normalizedHireDate.Value.Date || leaveJobDate.Value.Date > today)
            {
                AddError(issues, sourceRow.SourceRowNumber, "LeaveJobDate", "Ngày nghỉ việc không hợp lệ theo ngày vào làm.");
                return null;
            }

            return new EmploymentValues(
                EmployeeStatusCatalog.Resigned,
                normalizedHireDate.Value,
                null,
                ToDatabaseTimestamp(leaveJobDate.Value),
                usedFallbackHireDate);
        }

        AddError(issues, sourceRow.SourceRowNumber, "WorkStatus", "Tình trạng công tác chưa được hỗ trợ để import.");
        return null;
    }

    private static string? ValidateOptionalValue(
        DatabaseNhanSuWorkbookPreviewService.SourceEmployeeRow sourceRow,
        string? value,
        string field,
        int maximumLength,
        ICollection<NhanSuWorkbookImportIssueDto> issues)
    {
        var normalizedValue = NormalizeValue(value);
        if (normalizedValue is null)
        {
            return null;
        }

        if (IsSpreadsheetError(normalizedValue))
        {
            AddError(issues, sourceRow.SourceRowNumber, field, "Trường dữ liệu chứa kết quả công thức không hợp lệ.");
            return null;
        }

        if (normalizedValue.Length > maximumLength)
        {
            AddError(issues, sourceRow.SourceRowNumber, field, $"Trường dữ liệu không được vượt quá {maximumLength} ký tự.");
        }

        return normalizedValue;
    }

    private static DateTime? ParseOptionalDate(
        DatabaseNhanSuWorkbookPreviewService.SourceEmployeeRow sourceRow,
        string? value,
        string field,
        ICollection<NhanSuWorkbookImportIssueDto> issues)
    {
        var normalizedValue = NormalizeValue(value);
        if (normalizedValue is null)
        {
            return null;
        }

        if (IsSpreadsheetError(normalizedValue)
            || !DateTime.TryParseExact(
                normalizedValue,
                SourceDateFormats,
                VietnameseCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var parsedDate))
        {
            AddError(issues, sourceRow.SourceRowNumber, field, "Giá trị ngày không hợp lệ.");
            return null;
        }

        return ToDatabaseTimestamp(parsedDate);
    }

    private async Task LockEmployeesForImportIfNeededAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(
                dbContext.Database.ProviderName,
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                StringComparison.Ordinal))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
                "LOCK TABLE public.employees IN SHARE ROW EXCLUSIVE MODE;",
                cancellationToken)
            .ConfigureAwait(false);
    }

    private AuditCommand CreateAuditCommand()
    {
        var current = auditScope.Current;
        return new AuditCommand(
            current?.OperationId ?? Guid.NewGuid(),
            AuditActions.NhanVien.ImportFromNhanSuWorkbook,
            current?.Actor ?? new AuditActor(
                SystemActorId,
                SystemActorId,
                AuditActorKind.System,
                AuditSource.Worker),
            current?.CorrelationId ?? Guid.NewGuid().ToString("N"),
            AuditCaptureMode.OperationOnly,
            Metadata: current?.Metadata);
    }

    private static AuditOperationEvent CreateAuditEvent(NhanSuWorkbookImportResultDto result) =>
        new(
            AuditActions.NhanVien.ImportFromNhanSuWorkbook,
            AuditEntityTypes.EmployeeWorkbookImport,
            EntityId: NhanSuSheetName,
            EntityDisplayName: NhanSuSheetName,
            Outcome: result.CreatedCount == 0
                ? AuditOperationOutcome.NoChanges
                : AuditOperationOutcome.Succeeded,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["sourceRowCount"] = result.SourceRowCount.ToString(CultureInfo.InvariantCulture),
                ["createdCount"] = result.CreatedCount.ToString(CultureInfo.InvariantCulture),
                ["skippedExistingCount"] = result.SkippedExistingCount.ToString(CultureInfo.InvariantCulture),
                ["formulaErrorCount"] = result.FormulaErrorCount.ToString(CultureInfo.InvariantCulture),
                ["warningCount"] = result.Warnings.Count.ToString(CultureInfo.InvariantCulture),
                ["fallbackHireDateCount"] = result.FallbackHireDateCount.ToString(CultureInfo.InvariantCulture),
                ["sheet"] = NhanSuSheetName
            });

    private static DateTime? NormalizeFallbackHireDate(DateTime? fallbackHireDate)
    {
        if (!fallbackHireDate.HasValue)
        {
            return null;
        }

        var normalizedDate = ToDatabaseTimestamp(fallbackHireDate.Value);
        if (normalizedDate.Date > DateTime.Today)
        {
            throw new InvalidOperationException("Ngày vào làm xác nhận không được ở tương lai.");
        }

        return normalizedDate;
    }

    private static void ValidateActiveStatusFallback(int? activeStatusFallback)
    {
        if (activeStatusFallback.HasValue
            && activeStatusFallback is not EmployeeStatusCatalog.Probation and not EmployeeStatusCatalog.Official)
        {
            throw new InvalidOperationException("Trạng thái xác nhận chỉ có thể là thử việc hoặc chính thức.");
        }
    }

    private static string? NormalizeEmployeeCode(string? value) =>
        NormalizeValue(value)?.ToUpperInvariant();

    private static string? NormalizeMatchText(string? value) =>
        NormalizeValue(value)?.ToUpperInvariant();

    private static string? NormalizeValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? null : string.Join(" ", parts);
    }

    private static string? CreateDepartmentKey(string? centerName, string? departmentName)
    {
        var normalizedCenterName = NormalizeMatchText(centerName);
        var normalizedDepartmentName = NormalizeMatchText(departmentName);
        return normalizedCenterName is null || normalizedDepartmentName is null
            ? null
            : $"{normalizedCenterName}\u001F{normalizedDepartmentName}";
    }

    private static bool IsSpreadsheetError(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.StartsWith('#');

    private static DateTime ToDatabaseTimestamp(DateTime value) =>
        DateTime.SpecifyKind(value.Date, DateTimeKind.Unspecified);

    private static void AddError(
        ICollection<NhanSuWorkbookImportIssueDto> issues,
        int sourceRowNumber,
        string field,
        string message) =>
        issues.Add(new NhanSuWorkbookImportIssueDto(
            sourceRowNumber,
            field,
            NhanSuWorkbookImportIssueSeverity.Error,
            message));

    private static void AddWarning(
        ICollection<NhanSuWorkbookImportIssueDto> issues,
        int sourceRowNumber,
        string field,
        string message) =>
        issues.Add(new NhanSuWorkbookImportIssueDto(
            sourceRowNumber,
            field,
            NhanSuWorkbookImportIssueSeverity.Warning,
            message));

    private sealed record ExistingEmployee(Guid Id, string EmployeeCode);

    private sealed record DepartmentReference(Guid Id, string CenterName, string DepartmentName);

    private sealed record PositionReference(Guid Id, string Name);

    private sealed record EmploymentValues(
        int Status,
        DateTime HireDate,
        DateTime? SeniorityStartDate,
        DateTime? ResignedDate,
        bool UsedFallbackHireDate);
}
