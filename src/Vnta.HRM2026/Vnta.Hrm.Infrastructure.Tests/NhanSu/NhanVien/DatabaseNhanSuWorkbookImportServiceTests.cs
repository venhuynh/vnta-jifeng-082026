using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vnta.Hrm.Application.NhanSu.NhanVien;
using Vnta.Hrm.Application.QuanTri.AuditTrail;
using Vnta.Hrm.Infrastructure.Data;
using Vnta.Hrm.Infrastructure.NhanSu.ChucVu;
using Vnta.Hrm.Infrastructure.NhanSu.NhanVien;
using Vnta.Hrm.Infrastructure.NhanSu.PhongBan;
using Vnta.Hrm.Infrastructure.QuanTri.AuditTrail;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.NhanSu.NhanVien;

public sealed class DatabaseNhanSuWorkbookImportServiceTests
{
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace OfficeDocumentRelationshipNamespace =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace PackageRelationshipNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    [Fact]
    public async Task ImportAsync_maps_supported_fields_creates_employee_and_writes_aggregate_audit_event()
    {
        await using var dbContext = CreateDbContext();
        var references = await SeedReferencesAsync(dbContext);
        dbContext.ChangeTracker.Clear();
        using var workbook = CreateWorkbook(
            new SourceRow(
                7,
                " jf00156 ",
                "Nguyen Van An",
                "Manufacturing",
                "Packaging",
                "Packaging Operator",
                "Operator",
                "\u0110ang l\u00e0m vi\u1ec7c",
                "06/10/2025",
                "156",
                CompanyEmail: "an@example.test",
                Phone: "0900000000",
                OfficialStartDate: "06/11/2025",
                HasFormulaError: true));

        var result = await CreateService(dbContext).ImportAsync(workbook);

        Assert.Equal(1, result.SourceRowCount);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.SkippedExistingCount);
        Assert.Equal(1, result.FormulaErrorCount);
        Assert.Equal(0, result.FallbackHireDateCount);
        Assert.Empty(result.Warnings);

        var employee = await dbContext.Employees.AsNoTracking().SingleAsync();
        Assert.Equal("JF00156", employee.EmployeeCode);
        Assert.Equal("Nguyen Van", employee.LastName);
        Assert.Equal("An", employee.FirstName);
        Assert.Equal("an@example.test", employee.Email);
        Assert.Equal("0900000000", employee.PhoneNumber);
        Assert.Equal(references.DepartmentId, employee.DepartmentId);
        Assert.Equal(references.PositionId, employee.PositionId);
        Assert.Equal(EmployeeStatusCatalog.Official, employee.Status);
        Assert.Equal(new DateTime(2025, 10, 6), employee.HireDate);
        Assert.Equal(new DateTime(2025, 11, 6), employee.SeniorityStartDate);
        Assert.Null(employee.ResignedDate);
        Assert.False(employee.IsDeleted);

        Assert.Empty(await dbContext.EmployeeContactProfiles.AsNoTracking().ToListAsync());
        Assert.Empty(await dbContext.EmployeeCitizenIdentities.AsNoTracking().ToListAsync());

        var auditEvent = await dbContext.AuditEvents.AsNoTracking().SingleAsync();
        Assert.Equal(AuditActions.NhanVien.ImportFromNhanSuWorkbook, auditEvent.Action);
        Assert.Equal(AuditEntityTypes.EmployeeWorkbookImport, auditEvent.EntityType);
        Assert.Equal("NhanSu", auditEvent.EntityId);
        Assert.Contains("\"createdCount\":\"1\"", auditEvent.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain(employee.EmployeeCode, auditEvent.MetadataJson, StringComparison.Ordinal);
        Assert.DoesNotContain("Nguyen Van An", auditEvent.MetadataJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportAsync_prevalidates_entire_workbook_and_does_not_create_any_employee_when_a_row_is_invalid()
    {
        await using var dbContext = CreateDbContext();
        await SeedReferencesAsync(dbContext);
        dbContext.ChangeTracker.Clear();
        using var workbook = CreateWorkbook(
            CreateActiveOfficialSourceRow(7, "NEW001"),
            CreateActiveOfficialSourceRow(8, "NEW002") with { Title = "Unknown title" });

        var exception = await Assert.ThrowsAsync<NhanSuWorkbookImportValidationException>(
            () => CreateService(dbContext).ImportAsync(workbook));

        Assert.Contains(exception.Issues, issue =>
            issue.SourceRowNumber == 8
            && issue.Field == "TitleId"
            && issue.Severity == NhanSuWorkbookImportIssueSeverity.Error);
        Assert.Empty(await dbContext.Employees.AsNoTracking().ToListAsync());
        Assert.Empty(await dbContext.AuditEvents.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task ImportAsync_is_idempotent_and_never_overwrites_an_existing_active_employee()
    {
        await using var dbContext = CreateDbContext();
        await SeedReferencesAsync(dbContext);
        dbContext.ChangeTracker.Clear();

        using (var firstWorkbook = CreateWorkbook(CreateActiveOfficialSourceRow(7, "JF00200")))
        {
            var firstResult = await CreateService(dbContext).ImportAsync(firstWorkbook);
            Assert.Equal(1, firstResult.CreatedCount);
            Assert.Equal(0, firstResult.SkippedExistingCount);
        }

        var firstEmployee = await dbContext.Employees.AsNoTracking().SingleAsync();
        dbContext.ChangeTracker.Clear();

        using (var secondWorkbook = CreateWorkbook(CreateActiveOfficialSourceRow(7, " jf00200 ")))
        {
            var secondResult = await CreateService(dbContext).ImportAsync(secondWorkbook);
            Assert.Equal(0, secondResult.CreatedCount);
            Assert.Equal(1, secondResult.SkippedExistingCount);
        }

        var persistedEmployee = await dbContext.Employees.AsNoTracking().SingleAsync();
        Assert.Equal(firstEmployee.Id, persistedEmployee.Id);
        Assert.Equal(firstEmployee.CreatedAtUtc, persistedEmployee.CreatedAtUtc);
        Assert.Equal(firstEmployee.UpdatedAtUtc, persistedEmployee.UpdatedAtUtc);
        Assert.Equal("JF00200", persistedEmployee.EmployeeCode);
        Assert.Equal(2, await dbContext.AuditEvents.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task ImportAsync_uses_missing_date_and_active_status_fallbacks_only_when_explicitly_supplied()
    {
        await using var dbContext = CreateDbContext();
        await SeedReferencesAsync(dbContext);
        dbContext.ChangeTracker.Clear();

        using (var unapprovedWorkbook = CreateWorkbook(CreateActiveSourceWithoutEmploymentMilestones(7, "FALLBACK01")))
        {
            var exception = await Assert.ThrowsAsync<NhanSuWorkbookImportValidationException>(
                () => CreateService(dbContext).ImportAsync(unapprovedWorkbook));

            Assert.Contains(exception.Issues, issue =>
                issue.SourceRowNumber == 7
                && issue.Field == "HireDate"
                && issue.Severity == NhanSuWorkbookImportIssueSeverity.Error);
        }

        Assert.Empty(await dbContext.Employees.AsNoTracking().ToListAsync());
        Assert.Empty(await dbContext.AuditEvents.AsNoTracking().ToListAsync());
        dbContext.ChangeTracker.Clear();

        using var approvedWorkbook = CreateWorkbook(CreateActiveSourceWithoutEmploymentMilestones(7, "FALLBACK01"));
        var result = await CreateService(dbContext).ImportAsync(
            approvedWorkbook,
            missingHireDateFallback: new DateTime(2025, 1, 15),
            activeStatusFallback: EmployeeStatusCatalog.Official);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.FallbackHireDateCount);
        Assert.Contains(result.Warnings, issue => issue.Field == "HireDate");
        Assert.Contains(result.Warnings, issue => issue.Field == "Status");

        var employee = await dbContext.Employees.AsNoTracking().SingleAsync();
        Assert.Equal(new DateTime(2025, 1, 15), employee.HireDate);
        Assert.Equal(EmployeeStatusCatalog.Official, employee.Status);
        Assert.Null(employee.SeniorityStartDate);
    }

    private static DatabaseNhanSuWorkbookImportService CreateService(ApplicationDbContext dbContext)
    {
        var auditScope = new AsyncLocalAuditScope();
        return new DatabaseNhanSuWorkbookImportService(
            dbContext,
            auditScope,
            new AuditedMutation(dbContext, auditScope));
    }

    private static ApplicationDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"nhansu-workbook-import-{Guid.NewGuid():N}")
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static async Task<(Guid DepartmentId, Guid PositionId)> SeedReferencesAsync(ApplicationDbContext dbContext)
    {
        var departmentId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 1, 1);
        dbContext.Departments.Add(new AttendanceDepartmentRow
        {
            Id = departmentId,
            Code = "PACK",
            CenterName = "Manufacturing",
            DepartmentOrWorkshopName = "Packaging",
            Status = 0,
            CreatedAtUtc = createdAt
        });
        dbContext.Positions.Add(new AttendanceGatewayPositionRow
        {
            Id = positionId,
            Code = "POS-PACK",
            Name = "Packaging Operator",
            Status = 0,
            EmployeeCount = 0,
            CreatedAtUtc = createdAt
        });
        await dbContext.SaveChangesAsync();
        return (departmentId, positionId);
    }

    private static SourceRow CreateActiveOfficialSourceRow(int rowNumber, string employeeCode) => new(
        rowNumber,
        employeeCode,
        "Nguyen Van An",
        "Manufacturing",
        "Packaging",
        "Packaging Operator",
        "Operator",
        "\u0110ang l\u00e0m vi\u1ec7c",
        "06/10/2025",
        "156",
        OfficialStartDate: "06/11/2025");

    private static SourceRow CreateActiveSourceWithoutEmploymentMilestones(int rowNumber, string employeeCode) => new(
        rowNumber,
        employeeCode,
        "Nguyen Van An",
        "Manufacturing",
        "Packaging",
        "Packaging Operator",
        "Operator",
        "\u0110ang l\u00e0m vi\u1ec7c",
        StartWorkDate: null,
        AttendanceCode: "156");

    private static MemoryStream CreateWorkbook(params SourceRow[] rows)
    {
        var worksheetRows = new List<XElement>
        {
            CreateHeaderRow()
        };
        worksheetRows.AddRange(rows.Select(CreateSourceRow));

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
            WriteEntry(archive, "xl/worksheets/sheet1.xml", worksheet);
        }

        stream.Position = 0;
        return stream;
    }

    private static XElement CreateHeaderRow()
    {
        var headers = new (string Column, string Value)[]
        {
            ("B", "Code"),
            ("C", "Name"),
            ("F", "DepartmentLevel1"),
            ("G", "DepartmentLevel2"),
            ("H", "TitleId"),
            ("I", "PositionId"),
            ("M", "WorkStatus"),
            ("N", "CompanyEmail"),
            ("O", "Phone"),
            ("P", "ProbationDate"),
            ("Q", "OfficialStartDate"),
            ("R", "LeaveJobDate"),
            ("S", "ContractEffectiveDate"),
            ("EP", "StartWorkDate"),
            ("FD", "AttendanceCode")
        };

        return new XElement(
            SpreadsheetNamespace + "row",
            new XAttribute("r", 6),
            headers.Select(header => CreateInlineCell($"{header.Column}6", header.Value)));
    }

    private static XElement CreateSourceRow(SourceRow row)
    {
        var cells = new List<XElement>();
        AddInlineCell(cells, "B", row.RowNumber, row.EmployeeCode);
        AddInlineCell(cells, "C", row.RowNumber, row.FullName);
        AddInlineCell(cells, "F", row.RowNumber, row.DepartmentLevel1);
        AddInlineCell(cells, "G", row.RowNumber, row.DepartmentLevel2);
        AddInlineCell(cells, "H", row.RowNumber, row.Title);
        AddInlineCell(cells, "I", row.RowNumber, row.Position);
        AddInlineCell(cells, "M", row.RowNumber, row.WorkStatus);
        AddInlineCell(cells, "N", row.RowNumber, row.CompanyEmail);
        AddInlineCell(cells, "O", row.RowNumber, row.Phone);
        AddInlineCell(cells, "P", row.RowNumber, row.ProbationDate);
        AddInlineCell(cells, "Q", row.RowNumber, row.OfficialStartDate);
        AddInlineCell(cells, "R", row.RowNumber, row.LeaveJobDate);
        AddInlineCell(cells, "S", row.RowNumber, row.ContractEffectiveDate);
        AddInlineCell(cells, "EP", row.RowNumber, row.StartWorkDate);
        AddInlineCell(cells, "FD", row.RowNumber, row.AttendanceCode);
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

    private static void AddInlineCell(ICollection<XElement> cells, string column, int rowNumber, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            cells.Add(CreateInlineCell($"{column}{rowNumber}", value));
        }
    }

    private static XElement CreateInlineCell(string reference, string value) => new(
        SpreadsheetNamespace + "c",
        new XAttribute("r", reference),
        new XAttribute("t", "inlineStr"),
        new XElement(
            SpreadsheetNamespace + "is",
            new XElement(SpreadsheetNamespace + "t", value)));

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
        string? CompanyEmail = null,
        string? Phone = null,
        string? ProbationDate = null,
        string? OfficialStartDate = null,
        string? LeaveJobDate = null,
        string? ContractEffectiveDate = null,
        bool HasFormulaError = false);
}
