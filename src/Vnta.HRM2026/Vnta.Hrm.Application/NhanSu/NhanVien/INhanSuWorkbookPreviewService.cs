namespace Vnta.Hrm.Application.NhanSu.NhanVien;

/// <summary>
/// Đọc và đối soát nội dung sheet <c>NhanSu</c> với nhân viên đang hoạt động trong hệ thống.
/// Contract này chỉ phục vụ preview; implementation không được tạo, cập nhật hoặc xoá dữ liệu nhân viên.
/// </summary>
public interface INhanSuWorkbookPreviewService
{
    Task<NhanSuWorkbookPreviewDto> PreviewAsync(
        Stream workbookStream,
        CancellationToken cancellationToken = default);
}

/// <summary>Giới hạn an toàn cho một tệp nguồn <c>.xlsx</c> được preview.</summary>
public static class NhanSuWorkbookPreviewLimits
{
    public const long MaxWorkbookBytes = 10L * 1024 * 1024;
}

public sealed record NhanSuWorkbookPreviewDto(
    int SourceRowCount,
    int MatchedCount,
    int UnmatchedCount,
    int AmbiguousCount,
    int InvalidSourceCount,
    int FormulaErrorCount,
    IReadOnlyList<NhanSuWorkbookPreviewRowDto> Rows);

/// <summary>
/// Chỉ bao gồm các cột cần thiết để đối soát hồ sơ nhân viên. Dữ liệu định danh, ngân hàng,
/// lương, bảo hiểm, địa chỉ và thuế không được đưa ra preview.
/// </summary>
public sealed record NhanSuWorkbookPreviewRowDto(
    int SourceRowNumber,
    string? SourceEmployeeCode,
    string? SourceFullName,
    string? SourceDepartmentLevel1,
    string? SourceDepartmentLevel2,
    string? SourceTitle,
    string? SourcePosition,
    string? SourceWorkStatus,
    string? SourceStartWorkDate,
    string? SourceAttendanceCode,
    NhanSuWorkbookRowMatchStatus MatchStatus,
    Guid? ExistingEmployeeId,
    string? ExistingEmployeeCode,
    string? ExistingEmployeeFullName,
    IReadOnlyList<NhanSuWorkbookPreviewIssueDto> Issues);

public sealed record NhanSuWorkbookPreviewIssueDto(
    string Field,
    NhanSuWorkbookPreviewIssueSeverity Severity,
    string Message);

public enum NhanSuWorkbookRowMatchStatus
{
    Matched = 0,
    Unmatched = 1,
    Ambiguous = 2,
    InvalidSource = 3
}

public enum NhanSuWorkbookPreviewIssueSeverity
{
    Warning = 0,
    Error = 1
}
