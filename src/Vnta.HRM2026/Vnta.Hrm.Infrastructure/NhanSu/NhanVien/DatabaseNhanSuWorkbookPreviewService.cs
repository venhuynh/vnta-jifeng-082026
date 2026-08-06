using System.Buffers;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.Data;

namespace Vnta.Hrm.Infrastructure.NhanSu.NhanVien;

/// <summary>
/// Reader hẹp cho template Jifeng. Parser nguồn được dùng chung bởi preview và import; preview
/// chỉ truy vấn <see cref="ApplicationDbContext.Employees"/> bằng <c>AsNoTracking</c>.
/// </summary>
public sealed class DatabaseNhanSuWorkbookPreviewService(ApplicationDbContext dbContext)
    : INhanSuWorkbookPreviewService
{
    private const string NhanSuSheetName = "NhanSu";
    private const int MaxArchiveEntryCount = 128;
    private const long MaxArchiveEntryBytes = 16L * 1024 * 1024;
    private const long MaxArchiveUncompressedBytes = 50L * 1024 * 1024;
    private const int MaxSharedStringCount = 200000;
    private const int MaxSourceRowCount = 10000;
    private const long MaxXmlCharacters = 20L * 1024 * 1024;

    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeDocumentRelationshipNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    private static readonly string[] RequiredHeaders =
    [
        "Code",
        "Name",
        "DepartmentLevel1",
        "DepartmentLevel2",
        "TitleId",
        "PositionId",
        "WorkStatus",
        "StartWorkDate",
        "AttendanceCode"
    ];

    public async Task<NhanSuWorkbookPreviewDto> PreviewAsync(
        Stream workbookStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workbookStream);
        cancellationToken.ThrowIfCancellationRequested();

        var sourceRows = await ReadSourceRowsAsync(workbookStream, cancellationToken);
        var existingEmployees = await dbContext.Employees
            .AsNoTracking()
            .Where(employee => !employee.IsDeleted)
            .Select(employee => new ExistingEmployee(
                employee.Id,
                employee.EmployeeCode,
                employee.FirstName,
                employee.LastName))
            .ToListAsync(cancellationToken);

        var existingEmployeesByCode = existingEmployees
            .Where(employee => NormalizeEmployeeCode(employee.EmployeeCode) is not null)
            .GroupBy(employee => NormalizeEmployeeCode(employee.EmployeeCode)!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var duplicateSourceCodes = sourceRows
            .Select(row => NormalizeEmployeeCode(row.EmployeeCode))
            .Where(code => code is not null)
            .GroupBy(code => code!, StringComparer.Ordinal)
            .Where(group => group.Skip(1).Any())
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        var previewRows = new List<NhanSuWorkbookPreviewRowDto>(sourceRows.Count);
        foreach (var sourceRow in sourceRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var issues = new List<NhanSuWorkbookPreviewIssueDto>();
            if (sourceRow.FormulaErrorCount > 0)
            {
                issues.Add(new NhanSuWorkbookPreviewIssueDto(
                    "Formula",
                    NhanSuWorkbookPreviewIssueSeverity.Warning,
                    $"Dòng nguồn có {sourceRow.FormulaErrorCount} ô công thức chưa có kết quả hợp lệ."));
            }

            var normalizedCode = NormalizeEmployeeCode(sourceRow.EmployeeCode);
            var hasInvalidSource = false;
            if (normalizedCode is null)
            {
                hasInvalidSource = true;
                issues.Add(new NhanSuWorkbookPreviewIssueDto(
                    "Code",
                    NhanSuWorkbookPreviewIssueSeverity.Error,
                    "Mã nhân viên là bắt buộc."));
            }
            else if (IsSpreadsheetError(sourceRow.EmployeeCode))
            {
                hasInvalidSource = true;
                issues.Add(new NhanSuWorkbookPreviewIssueDto(
                    "Code",
                    NhanSuWorkbookPreviewIssueSeverity.Error,
                    "Mã nhân viên chứa kết quả công thức không hợp lệ."));
            }
            else if (duplicateSourceCodes.Contains(normalizedCode))
            {
                hasInvalidSource = true;
                issues.Add(new NhanSuWorkbookPreviewIssueDto(
                    "Code",
                    NhanSuWorkbookPreviewIssueSeverity.Error,
                    "Mã nhân viên bị lặp trong sheet NhanSu."));
            }

            if (string.IsNullOrWhiteSpace(sourceRow.FullName))
            {
                hasInvalidSource = true;
                issues.Add(new NhanSuWorkbookPreviewIssueDto(
                    "Name",
                    NhanSuWorkbookPreviewIssueSeverity.Error,
                    "Họ tên nhân viên là bắt buộc."));
            }
            else if (IsSpreadsheetError(sourceRow.FullName))
            {
                hasInvalidSource = true;
                issues.Add(new NhanSuWorkbookPreviewIssueDto(
                    "Name",
                    NhanSuWorkbookPreviewIssueSeverity.Error,
                    "Họ tên chứa kết quả công thức không hợp lệ."));
            }

            var matchStatus = NhanSuWorkbookRowMatchStatus.InvalidSource;
            ExistingEmployee? matchedEmployee = null;
            if (!hasInvalidSource && normalizedCode is not null)
            {
                if (!existingEmployeesByCode.TryGetValue(normalizedCode, out var candidates))
                {
                    matchStatus = NhanSuWorkbookRowMatchStatus.Unmatched;
                    issues.Add(new NhanSuWorkbookPreviewIssueDto(
                        "Code",
                        NhanSuWorkbookPreviewIssueSeverity.Warning,
                        "Chưa tìm thấy nhân viên đang hoạt động có mã tương ứng trong hệ thống."));
                }
                else if (candidates.Length > 1)
                {
                    matchStatus = NhanSuWorkbookRowMatchStatus.Ambiguous;
                    issues.Add(new NhanSuWorkbookPreviewIssueDto(
                        "Code",
                        NhanSuWorkbookPreviewIssueSeverity.Error,
                        $"Tìm thấy {candidates.Length} nhân viên đang hoạt động có cùng mã trong hệ thống."));
                }
                else
                {
                    matchStatus = NhanSuWorkbookRowMatchStatus.Matched;
                    matchedEmployee = candidates[0];
                }
            }

            previewRows.Add(new NhanSuWorkbookPreviewRowDto(
                sourceRow.SourceRowNumber,
                sourceRow.EmployeeCode,
                sourceRow.FullName,
                sourceRow.DepartmentLevel1,
                sourceRow.DepartmentLevel2,
                sourceRow.Title,
                sourceRow.Position,
                sourceRow.WorkStatus,
                sourceRow.StartWorkDate,
                sourceRow.AttendanceCode,
                matchStatus,
                matchedEmployee?.Id,
                matchedEmployee?.EmployeeCode,
                matchedEmployee is null ? null : BuildFullName(matchedEmployee),
                issues));
        }

        return new NhanSuWorkbookPreviewDto(
            previewRows.Count,
            previewRows.Count(row => row.MatchStatus == NhanSuWorkbookRowMatchStatus.Matched),
            previewRows.Count(row => row.MatchStatus == NhanSuWorkbookRowMatchStatus.Unmatched),
            previewRows.Count(row => row.MatchStatus == NhanSuWorkbookRowMatchStatus.Ambiguous),
            previewRows.Count(row => row.MatchStatus == NhanSuWorkbookRowMatchStatus.InvalidSource),
            sourceRows.Sum(row => row.FormulaErrorCount),
            previewRows);
    }

    internal static async Task<IReadOnlyList<SourceEmployeeRow>> ReadSourceRowsAsync(
        Stream workbookStream,
        CancellationToken cancellationToken)
    {
        await using var bufferedWorkbook = await CopyWorkbookAsync(workbookStream, cancellationToken);
        try
        {
            using var archive = new ZipArchive(bufferedWorkbook, ZipArchiveMode.Read, leaveOpen: true);
            ValidateArchive(archive);

            var workbook = LoadXml(GetRequiredEntry(archive, "xl/workbook.xml"));
            var relationships = LoadXml(GetRequiredEntry(archive, "xl/_rels/workbook.xml.rels"));
            var sharedStrings = ReadSharedStrings(archive);
            var worksheet = LoadXml(GetNhanSuWorksheetEntry(archive, workbook, relationships));
            return ReadSourceRows(worksheet, sharedStrings, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidOperationException("Tệp tải lên không phải định dạng .xlsx hợp lệ.", exception);
        }
        catch (XmlException exception)
        {
            throw new InvalidOperationException("Tệp tải lên có cấu trúc XML không hợp lệ.", exception);
        }
    }

    private static async Task<MemoryStream> CopyWorkbookAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        var result = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            var totalBytesRead = 0L;
            while (true)
            {
                var read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                {
                    break;
                }

                totalBytesRead += read;
                if (totalBytesRead > NhanSuWorkbookPreviewLimits.MaxWorkbookBytes)
                {
                    throw new InvalidOperationException(
                        $"Tệp Excel không được vượt quá {NhanSuWorkbookPreviewLimits.MaxWorkbookBytes / (1024 * 1024)} MB.");
                }

                await result.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (totalBytesRead == 0)
            {
                throw new InvalidOperationException("Tệp Excel không có dữ liệu.");
            }

            result.Position = 0;
            return result;
        }
        catch
        {
            await result.DisposeAsync();
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static void ValidateArchive(ZipArchive archive)
    {
        if (archive.Entries.Count == 0 || archive.Entries.Count > MaxArchiveEntryCount)
        {
            throw new InvalidOperationException("Tệp Excel có số lượng thành phần không hợp lệ.");
        }

        var totalUncompressedBytes = 0L;
        foreach (var entry in archive.Entries)
        {
            if (entry.Length > MaxArchiveEntryBytes)
            {
                throw new InvalidOperationException("Tệp Excel có thành phần vượt quá giới hạn cho phép.");
            }

            totalUncompressedBytes += entry.Length;
            if (totalUncompressedBytes > MaxArchiveUncompressedBytes)
            {
                throw new InvalidOperationException("Tệp Excel có dung lượng giải nén vượt quá giới hạn cho phép.");
            }
        }
    }

    private static IReadOnlyList<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return [];
        }

        var document = LoadXml(entry);
        var items = document.Root?
            .Elements(SpreadsheetNamespace + "si")
            .ToArray()
            ?? [];
        if (items.Length > MaxSharedStringCount)
        {
            throw new InvalidOperationException("Tệp Excel có quá nhiều chuỗi dùng chung để xử lý an toàn.");
        }

        return items
            .Select(item => string.Concat(item.Descendants(SpreadsheetNamespace + "t").Select(text => text.Value)))
            .ToArray();
    }

    private static ZipArchiveEntry GetNhanSuWorksheetEntry(
        ZipArchive archive,
        XDocument workbook,
        XDocument relationships)
    {
        var sheets = workbook.Root?
            .Element(SpreadsheetNamespace + "sheets")?
            .Elements(SpreadsheetNamespace + "sheet")
            .Where(sheet => string.Equals(
                (string?)sheet.Attribute("name"),
                NhanSuSheetName,
                StringComparison.Ordinal))
            .ToArray()
            ?? [];
        if (sheets.Length != 1)
        {
            throw new InvalidOperationException("Không tìm thấy duy nhất sheet NhanSu trong tệp Excel.");
        }

        var relationshipId = (string?)sheets[0].Attribute(OfficeDocumentRelationshipNamespace + "id");
        if (string.IsNullOrWhiteSpace(relationshipId))
        {
            throw new InvalidOperationException("Sheet NhanSu không có liên kết dữ liệu hợp lệ.");
        }

        var relationship = relationships.Root?
            .Elements(PackageRelationshipNamespace + "Relationship")
            .SingleOrDefault(item => string.Equals(
                (string?)item.Attribute("Id"),
                relationshipId,
                StringComparison.Ordinal));
        var target = (string?)relationship?.Attribute("Target");
        if (string.IsNullOrWhiteSpace(target))
        {
            throw new InvalidOperationException("Không xác định được dữ liệu của sheet NhanSu.");
        }

        var worksheetPath = ResolveWorkbookTarget(target);
        return GetRequiredEntry(archive, worksheetPath);
    }

    private static string ResolveWorkbookTarget(string target)
    {
        var normalizedTarget = target.Replace('\\', '/').Trim();
        if (normalizedTarget.Length == 0
            || normalizedTarget.Contains("..", StringComparison.Ordinal)
            || normalizedTarget.Contains(':', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Liên kết sheet trong tệp Excel không hợp lệ.");
        }

        normalizedTarget = normalizedTarget.TrimStart('/');
        return normalizedTarget.StartsWith("xl/", StringComparison.Ordinal)
            ? normalizedTarget
            : $"xl/{normalizedTarget}";
    }

    private static ZipArchiveEntry GetRequiredEntry(ZipArchive archive, string path) =>
        archive.GetEntry(path)
        ?? throw new InvalidOperationException("Tệp Excel thiếu thành phần bắt buộc.");

    private static XDocument LoadXml(ZipArchiveEntry entry)
    {
        using var entryStream = entry.Open();
        using var reader = XmlReader.Create(entryStream, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxXmlCharacters,
            MaxCharactersFromEntities = 0
        });
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static IReadOnlyList<SourceEmployeeRow> ReadSourceRows(
        XDocument worksheet,
        IReadOnlyList<string> sharedStrings,
        CancellationToken cancellationToken)
    {
        var sheetData = worksheet.Root?.Element(SpreadsheetNamespace + "sheetData")
            ?? throw new InvalidOperationException("Sheet NhanSu không có vùng dữ liệu hợp lệ.");

        Dictionary<string, int>? headers = null;
        var sourceRows = new List<SourceEmployeeRow>();
        foreach (var row in sheetData.Elements(SpreadsheetNamespace + "row"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!int.TryParse((string?)row.Attribute("r"), out var rowNumber) || rowNumber <= 0)
            {
                continue;
            }

            var cells = ReadCells(row, sharedStrings);
            if (headers is null)
            {
                var candidateHeaders = BuildHeaders(cells);
                if (!candidateHeaders.ContainsKey("Code") || !candidateHeaders.ContainsKey("Name"))
                {
                    continue;
                }

                EnsureRequiredHeaders(candidateHeaders);
                headers = candidateHeaders;
                continue;
            }

            var sourceRow = CreateSourceRow(rowNumber, cells, headers);
            if (!sourceRow.HasSourceContent)
            {
                continue;
            }

            sourceRows.Add(sourceRow);
            if (sourceRows.Count > MaxSourceRowCount)
            {
                throw new InvalidOperationException("Sheet NhanSu có quá nhiều dòng để preview an toàn.");
            }
        }

        if (headers is null)
        {
            throw new InvalidOperationException("Không tìm thấy hàng tiêu đề Code và Name trong sheet NhanSu.");
        }

        return sourceRows;
    }

    private static Dictionary<int, WorkbookCell> ReadCells(
        XElement row,
        IReadOnlyList<string> sharedStrings)
    {
        var cells = new Dictionary<int, WorkbookCell>();
        foreach (var cell in row.Elements(SpreadsheetNamespace + "c"))
        {
            var columnIndex = GetColumnIndex((string?)cell.Attribute("r"));
            if (columnIndex is null)
            {
                continue;
            }

            cells[columnIndex.Value] = ReadCell(cell, sharedStrings);
        }

        return cells;
    }

    private static WorkbookCell ReadCell(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = (string?)cell.Attribute("t");
        var value = type switch
        {
            "s" => ReadSharedString(cell, sharedStrings),
            "inlineStr" => string.Concat(
                cell.Element(SpreadsheetNamespace + "is")?
                    .Descendants(SpreadsheetNamespace + "t")
                    .Select(text => text.Value)
                ?? []),
            _ => (string?)cell.Element(SpreadsheetNamespace + "v")
        };
        var hasFormulaError = string.Equals(type, "e", StringComparison.Ordinal)
            || (cell.Element(SpreadsheetNamespace + "f") is not null && IsSpreadsheetError(value));
        return new WorkbookCell(NormalizeValue(value), hasFormulaError);
    }

    private static string? ReadSharedString(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var rawIndex = (string?)cell.Element(SpreadsheetNamespace + "v");
        if (!int.TryParse(rawIndex, out var index) || index < 0 || index >= sharedStrings.Count)
        {
            throw new InvalidOperationException("Tệp Excel có chỉ mục chuỗi dùng chung không hợp lệ.");
        }

        return sharedStrings[index];
    }

    private static int? GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return null;
        }

        var result = 0;
        var foundColumnCharacter = false;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            var normalizedCharacter = char.ToUpperInvariant(character);
            if (normalizedCharacter is < 'A' or > 'Z')
            {
                return null;
            }

            var columnValue = normalizedCharacter - 'A' + 1;
            if (result > (16384 - columnValue) / 26)
            {
                return null;
            }

            foundColumnCharacter = true;
            result = (result * 26) + columnValue;
        }

        return foundColumnCharacter ? result : null;
    }

    private static Dictionary<string, int> BuildHeaders(IReadOnlyDictionary<int, WorkbookCell> cells)
    {
        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (columnIndex, cell) in cells)
        {
            if (!string.IsNullOrWhiteSpace(cell.Value))
            {
                headers.TryAdd(cell.Value, columnIndex);
            }
        }

        return headers;
    }

    private static void EnsureRequiredHeaders(IReadOnlyDictionary<string, int> headers)
    {
        var missingHeaders = RequiredHeaders
            .Where(header => !headers.ContainsKey(header))
            .ToArray();
        if (missingHeaders.Length > 0)
        {
            throw new InvalidOperationException(
                $"Sheet NhanSu thiếu cột bắt buộc: {string.Join(", ", missingHeaders)}.");
        }
    }

    private static SourceEmployeeRow CreateSourceRow(
        int rowNumber,
        IReadOnlyDictionary<int, WorkbookCell> cells,
        IReadOnlyDictionary<string, int> headers)
    {
        var employeeCode = GetCellValue(cells, headers, "Code");
        var fullName = GetCellValue(cells, headers, "Name");
        var departmentLevel1 = GetCellValue(cells, headers, "DepartmentLevel1");
        var departmentLevel2 = GetCellValue(cells, headers, "DepartmentLevel2");
        var title = GetCellValue(cells, headers, "TitleId");
        var position = GetCellValue(cells, headers, "PositionId");
        var workStatus = GetCellValue(cells, headers, "WorkStatus");
        var startWorkDate = GetCellValue(cells, headers, "StartWorkDate");
        var attendanceCode = GetCellValue(cells, headers, "AttendanceCode");
        var companyEmail = GetCellValue(cells, headers, "CompanyEmail");
        var phone = GetCellValue(cells, headers, "Phone");
        var probationDate = GetCellValue(cells, headers, "ProbationDate");
        var officialStartDate = GetCellValue(cells, headers, "OfficialStartDate");
        var leaveJobDate = GetCellValue(cells, headers, "LeaveJobDate");
        var contractEffectiveDate = GetCellValue(cells, headers, "ContractEffectiveDate");
        var hasSourceContent =
            !string.IsNullOrWhiteSpace(employeeCode)
            || !string.IsNullOrWhiteSpace(fullName)
            || !string.IsNullOrWhiteSpace(departmentLevel1)
            || !string.IsNullOrWhiteSpace(departmentLevel2)
            || !string.IsNullOrWhiteSpace(title)
            || !string.IsNullOrWhiteSpace(position)
            || !string.IsNullOrWhiteSpace(workStatus)
            || !string.IsNullOrWhiteSpace(startWorkDate)
            || !string.IsNullOrWhiteSpace(attendanceCode);

        return new SourceEmployeeRow(
            rowNumber,
            employeeCode,
            fullName,
            departmentLevel1,
            departmentLevel2,
            title,
            position,
            workStatus,
            startWorkDate,
            attendanceCode,
            companyEmail,
            phone,
            probationDate,
            officialStartDate,
            leaveJobDate,
            contractEffectiveDate,
            hasSourceContent,
            cells.Values.Count(cell => cell.HasFormulaError));
    }

    private static string? GetCellValue(
        IReadOnlyDictionary<int, WorkbookCell> cells,
        IReadOnlyDictionary<string, int> headers,
        string header) =>
        headers.TryGetValue(header, out var columnIndex)
        && cells.TryGetValue(columnIndex, out var cell)
            ? cell.Value
            : null;

    private static string? NormalizeValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeEmployeeCode(string? employeeCode) =>
        NormalizeValue(employeeCode)?.ToUpperInvariant();

    private static bool IsSpreadsheetError(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith('#');

    private static string? BuildFullName(ExistingEmployee employee)
    {
        var fullName = string.Join(
            " ",
            new[] { employee.LastName, employee.FirstName }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        return NormalizeValue(fullName);
    }

    private sealed record WorkbookCell(string? Value, bool HasFormulaError);

    internal sealed record SourceEmployeeRow(
        int SourceRowNumber,
        string? EmployeeCode,
        string? FullName,
        string? DepartmentLevel1,
        string? DepartmentLevel2,
        string? Title,
        string? Position,
        string? WorkStatus,
        string? StartWorkDate,
        string? AttendanceCode,
        string? CompanyEmail,
        string? Phone,
        string? ProbationDate,
        string? OfficialStartDate,
        string? LeaveJobDate,
        string? ContractEffectiveDate,
        bool HasSourceContent,
        int FormulaErrorCount);

    private sealed record ExistingEmployee(
        Guid Id,
        string EmployeeCode,
        string FirstName,
        string LastName);
}
