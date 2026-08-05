namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public sealed record EmployeeSummaryDto(
    int TotalCount,
    int WorkingCount,
    int ProbationCount,
    int OfficialCount,
    int ResignedCount);
