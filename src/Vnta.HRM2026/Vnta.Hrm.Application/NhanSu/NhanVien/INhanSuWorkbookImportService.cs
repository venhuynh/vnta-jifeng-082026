namespace Vnta.Hrm.Application.NhanSu.NhanVien;

/// <summary>
/// Imports the supported employee fields from the Jifeng <c>NhanSu</c> worksheet.
/// Implementations must validate the whole workbook before creating any employee.
/// </summary>
public interface INhanSuWorkbookImportService
{
    /// <summary>
    /// Creates employees that do not already have an active, case-insensitively matching code.
    /// Existing employees are deliberately left unchanged.
    /// </summary>
    /// <param name="workbookStream">Raw <c>.xlsx</c> content.</param>
    /// <param name="missingHireDateFallback">
    /// An explicitly approved date used only when a source row has no usable employment date.
    /// A null value keeps the import fail-closed for such rows.
    /// </param>
    /// <param name="activeStatusFallback">
    /// An explicitly approved probation or official status used only for a currently-working source
    /// row that has neither a probation nor an official-start date. A null value keeps the import
    /// fail-closed for such rows.
    /// </param>
    Task<NhanSuWorkbookImportResultDto> ImportAsync(
        Stream workbookStream,
        DateTime? missingHireDateFallback = null,
        int? activeStatusFallback = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Safe, aggregate result of a create-only workbook import.</summary>
public sealed record NhanSuWorkbookImportResultDto(
    int SourceRowCount,
    int CreatedCount,
    int SkippedExistingCount,
    int FormulaErrorCount,
    int FallbackHireDateCount,
    IReadOnlyList<NhanSuWorkbookImportIssueDto> Warnings);

/// <summary>
/// A row-level import issue. It intentionally excludes source employee code, name and other PII.
/// </summary>
public sealed record NhanSuWorkbookImportIssueDto(
    int SourceRowNumber,
    string Field,
    NhanSuWorkbookImportIssueSeverity Severity,
    string Message);

public enum NhanSuWorkbookImportIssueSeverity
{
    Warning = 0,
    Error = 1
}

/// <summary>
/// Indicates that source preflight failed. Callers can return its safe issues without exposing
/// workbook values.
/// </summary>
public sealed class NhanSuWorkbookImportValidationException(
    IReadOnlyList<NhanSuWorkbookImportIssueDto> issues)
    : InvalidOperationException("Tệp Excel chưa đủ điều kiện để import nhân viên.")
{
    public IReadOnlyList<NhanSuWorkbookImportIssueDto> Issues { get; } = issues;
}
