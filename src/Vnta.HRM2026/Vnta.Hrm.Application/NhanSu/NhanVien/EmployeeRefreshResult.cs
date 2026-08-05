namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public sealed record EmployeeRefreshResult(
    bool SourceAvailable,
    int SourceRowCount,
    int CreatedCount,
    int UpdatedCount,
    int SkippedCount,
    DateTime RefreshedAtUtc,
    string? Note);
