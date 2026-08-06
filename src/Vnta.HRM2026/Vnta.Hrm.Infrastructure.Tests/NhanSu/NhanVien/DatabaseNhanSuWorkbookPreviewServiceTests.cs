using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Vnta.Hrm.Application.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.NhanSu.NhanVien;

public sealed class DatabaseNhanSuWorkbookPreviewServiceTests
{
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeDocumentRelationshipNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    [Fact]
    public async Task PreviewAsync_reconciles_source_rows_and_does_not_change_employees()
    {
        await using var dbContext = CreateDbContext();
        await AddEmployeeAsync(dbContext, "JF00156", "Nguyễn Thị Thúy", "An");
        dbContext.ChangeTracker.Clear();
        using var workbook = CreateWorkbook(
            new SourceRow(
                7,
                " jf00156 ",
                "Nguyễn Thị Thúy An",
                "Hành Chính Nhân Sự/ Tuân thủ",
                "An toàn và tuân thủ",
                "Nhân viên HSE",
                "Nhân viên",
                "Đang làm việc",
                "06/10/2025",
                "156",
                HasFormulaError: true),
            new SourceRow(
                8,
                "NEW001",
                "Nguyễn Văn B",
                "Sản Xuất",
                null,
                "Nhân viên sản xuất",
                "Nhân viên",
                "Đang làm việc",
                "01/09/2025",
                "200"),
            new SourceRow(9, "DUP001", "Nguyễn Văn C", null, null, null, null, null, null, null),
            new SourceRow(10, " dup001 ", "Nguyễn Văn D", null, null, null, null, null, null, null),
            new SourceRow(11, null, "Thiếu mã", null, null, null, null, null, null, null));

        var result = await CreateService(dbContext).PreviewAsync(workbook);

        Assert.Equal(5, result.SourceRowCount);
        Assert.Equal(1, result.MatchedCount);
        Assert.Equal(1, result.UnmatchedCount);
        Assert.Equal(0, result.AmbiguousCount);
        Assert.Equal(3, result.InvalidSourceCount);
        Assert.Equal(1, result.FormulaErrorCount);

        var matched = Assert.Single(result.Rows, row => row.SourceRowNumber == 7);
        Assert.Equal(NhanSuWorkbookRowMatchStatus.Matched, matched.MatchStatus);
        Assert.Equal("jf00156", matched.SourceEmployeeCode);
        Assert.Equal("JF00156", matched.ExistingEmployeeCode);
        Assert.Equal("Nguyễn Thị Thúy An", matched.ExistingEmployeeFullName);
        Assert.Equal("Hành Chính Nhân Sự/ Tuân thủ", matched.SourceDepartmentLevel1);
        Assert.Equal("An toàn và tuân thủ", matched.SourceDepartmentLevel2);
        Assert.Equal("06/10/2025", matched.SourceStartWorkDate);
        Assert.Contains(matched.Issues, issue =>
            issue.Field == "Formula"
            && issue.Severity == NhanSuWorkbookPreviewIssueSeverity.Warning);

        Assert.Equal(
            NhanSuWorkbookRowMatchStatus.Unmatched,
            Assert.Single(result.Rows, row => row.SourceRowNumber == 8).MatchStatus);
        Assert.All(
            result.Rows.Where(row => row.SourceRowNumber is 9 or 10),
            row => Assert.Equal(NhanSuWorkbookRowMatchStatus.InvalidSource, row.MatchStatus));
        Assert.Equal(
            NhanSuWorkbookRowMatchStatus.InvalidSource,
            Assert.Single(result.Rows, row => row.SourceRowNumber == 11).MatchStatus);

        Assert.Empty(dbContext.ChangeTracker.Entries());
        var persistedEmployee = await dbContext.Employees.AsNoTracking().SingleAsync();
        Assert.Equal("JF00156", persistedEmployee.EmployeeCode);
    }

    [Fact]
    public async Task PreviewAsync_marks_multiple_active_employee_matches_as_ambiguous()
    {
        await using var dbContext = CreateDbContext();
        await AddEmployeeAsync(dbContext, "DUPDB", "Nguyễn", "A");
        await AddEmployeeAsync(dbContext, " dupdb ", "Nguyễn", "B");
        dbContext.ChangeTracker.Clear();
        using var workbook = CreateWorkbook(
            new SourceRow(7, "dupdb", "Nguyễn Văn A", null, null, null, null, null, null, null));

        var result = await CreateService(dbContext).PreviewAsync(workbook);

        var row = Assert.Single(result.Rows);
        Assert.Equal(NhanSuWorkbookRowMatchStatus.Ambiguous, row.MatchStatus);
        Assert.Null(row.ExistingEmployeeId);
        Assert.Contains(row.Issues, issue =>
            issue.Field == "Code"
            && issue.Severity == NhanSuWorkbookPreviewIssueSeverity.Error
            && issue.Message.Contains("2", StringComparison.Ordinal));
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    [Fact]
    public async Task PreviewAsync_rejects_a_template_missing_required_headers()
    {
        await using var dbContext = CreateDbContext();
        using var workbook = CreateWorkbook(
            includeAttendanceCode: false,
            new SourceRow(7, "JF00156", "Nguyễn Thị Thúy An", null, null, null, null, null, null, null));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateService(dbContext).PreviewAsync(workbook));

        Assert.Contains("AttendanceCode", exception.Message, StringComparison.Ordinal);
        Assert.Empty(dbContext.ChangeTracker.Entries());
    }

    private static DatabaseNhanSuWorkbookPreviewService CreateService(ApplicationDbContext dbContext) => new(dbContext);

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"nhansu-workbook-preview-{Guid.NewGuid():N}")
            .Options);

    private static async Task AddEmployeeAsync(
        ApplicationDbContext dbContext,
        string employeeCode,
        string lastName,
        string firstName)
    {
        dbContext.Employees.Add(new AttendanceGatewayEmployeeRow
        {
            Id = Guid.NewGuid(),
            EmployeeCode = employeeCode,
            LastName = lastName,
            FirstName = firstName,
            CreatedAtUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();
    }

    private static MemoryStream CreateWorkbook(params SourceRow[] rows) =>
        CreateWorkbook(includeAttendanceCode: true, rows);

    private static MemoryStream CreateWorkbook(bool includeAttendanceCode, params SourceRow[] rows)
    {
        var sharedStrings = new SharedStringTable();
        var worksheetRows = new List<XElement>
        {
            CreateHeaderRow(sharedStrings, includeAttendanceCode)
        };
        worksheetRows.AddRange(rows.Select(row => CreateSourceRow(sharedStrings, row)));
        worksheetRows.Add(new XElement(
            SpreadsheetNamespace + "row",
            new XAttribute("r", 211),
            new XElement(
                SpreadsheetNamespace + "c",
                new XAttribute("r", "DH211"),
                new XAttribute("t", "e"),
                new XElement(SpreadsheetNamespace + "f", "XLOOKUP()"),
                new XElement(SpreadsheetNamespace + "v", "#N/A"))));

        var workbook = new XDocument(
            new XElement(
                SpreadsheetNamespace + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", OfficeDocumentRelationshipNamespace),
                new XElement(
                    SpreadsheetNamespace + "sheets",
                    new XElement(
                        SpreadsheetNamespace + "sheet",
                        new XAttribute("name", "NhanSu"),
                        new XAttribute("sheetId", "1"),
                        new XAttribute(OfficeDocumentRelationshipNamespace + "id", "rId1")))));
        var relationships = new XDocument(
            new XElement(
                PackageRelationshipNamespace + "Relationships",
                new XElement(
                    PackageRelationshipNamespace + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", "worksheets/sheet1.xml"))));
        var worksheet = new XDocument(
            new XElement(
                SpreadsheetNamespace + "worksheet",
                new XElement(SpreadsheetNamespace + "sheetData", worksheetRows)));

        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "xl/workbook.xml", workbook);
            WriteEntry(archive, "xl/_rels/workbook.xml.rels", relationships);
            WriteEntry(archive, "xl/sharedStrings.xml", sharedStrings.ToDocument());
            WriteEntry(archive, "xl/worksheets/sheet1.xml", worksheet);
        }

        stream.Position = 0;
        return stream;
    }

    private static XElement CreateHeaderRow(SharedStringTable sharedStrings, bool includeAttendanceCode)
    {
        var headers = new List<(string Column, string Value)>
        {
            ("B", "Code"),
            ("C", "Name"),
            ("F", "DepartmentLevel1"),
            ("G", "DepartmentLevel2"),
            ("H", "TitleId"),
            ("I", "PositionId"),
            ("M", "WorkStatus"),
            ("EP", "StartWorkDate")
        };
        if (includeAttendanceCode)
        {
            headers.Add(("FD", "AttendanceCode"));
        }

        return new XElement(
            SpreadsheetNamespace + "row",
            new XAttribute("r", 6),
            headers.Select(header => sharedStrings.CreateCell($"{header.Column}6", header.Value)));
    }

    private static XElement CreateSourceRow(SharedStringTable sharedStrings, SourceRow row)
    {
        var cells = new List<XElement>();
        AddSharedCell(cells, sharedStrings, "B", row.RowNumber, row.EmployeeCode);
        AddSharedCell(cells, sharedStrings, "C", row.RowNumber, row.FullName);
        AddSharedCell(cells, sharedStrings, "F", row.RowNumber, row.DepartmentLevel1);
        if (!string.IsNullOrWhiteSpace(row.DepartmentLevel2))
        {
            cells.Add(sharedStrings.CreateInlineCell($"G{row.RowNumber}", row.DepartmentLevel2));
        }

        AddSharedCell(cells, sharedStrings, "H", row.RowNumber, row.Title);
        AddSharedCell(cells, sharedStrings, "I", row.RowNumber, row.Position);
        AddSharedCell(cells, sharedStrings, "M", row.RowNumber, row.WorkStatus);
        AddSharedCell(cells, sharedStrings, "EP", row.RowNumber, row.StartWorkDate);
        AddSharedCell(cells, sharedStrings, "FD", row.RowNumber, row.AttendanceCode);
        if (row.HasFormulaError)
        {
            cells.Add(new XElement(
                SpreadsheetNamespace + "c",
                new XAttribute("r", $"DH{row.RowNumber}"),
                new XAttribute("t", "e"),
                new XElement(SpreadsheetNamespace + "f", "XLOOKUP()"),
                new XElement(SpreadsheetNamespace + "v", "#N/A")));
        }

        return new XElement(
            SpreadsheetNamespace + "row",
            new XAttribute("r", row.RowNumber),
            cells);
    }

    private static void AddSharedCell(
        ICollection<XElement> cells,
        SharedStringTable sharedStrings,
        string column,
        int rowNumber,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            cells.Add(sharedStrings.CreateCell($"{column}{rowNumber}", value));
        }
    }

    private static void WriteEntry(ZipArchive archive, string path, XDocument document)
    {
        var entry = archive.CreateEntry(path);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(document.ToString(SaveOptions.DisableFormatting));
    }

    private sealed record SourceRow(
        int RowNumber,
        string? EmployeeCode,
        string? FullName,
        string? DepartmentLevel1,
        string? DepartmentLevel2,
        string? Title,
        string? Position,
        string? WorkStatus,
        string? StartWorkDate,
        string? AttendanceCode,
        bool HasFormulaError = false);

    private sealed class SharedStringTable
    {
        private readonly Dictionary<string, int> indexes = new(StringComparer.Ordinal);
        private readonly List<string> values = [];

        public XElement CreateCell(string reference, string value) =>
            new(
                SpreadsheetNamespace + "c",
                new XAttribute("r", reference),
                new XAttribute("t", "s"),
                new XElement(SpreadsheetNamespace + "v", GetIndex(value)));

        public XElement CreateInlineCell(string reference, string value) =>
            new(
                SpreadsheetNamespace + "c",
                new XAttribute("r", reference),
                new XAttribute("t", "inlineStr"),
                new XElement(
                    SpreadsheetNamespace + "is",
                    new XElement(SpreadsheetNamespace + "t", value)));

        public XDocument ToDocument() =>
            new(
                new XElement(
                    SpreadsheetNamespace + "sst",
                    new XAttribute("count", values.Count),
                    new XAttribute("uniqueCount", values.Count),
                    values.Select(value => new XElement(
                        SpreadsheetNamespace + "si",
                        new XElement(SpreadsheetNamespace + "t", value)))));

        private int GetIndex(string value)
        {
            if (indexes.TryGetValue(value, out var index))
            {
                return index;
            }

            index = values.Count;
            values.Add(value);
            indexes[value] = index;
            return index;
        }
    }
}
