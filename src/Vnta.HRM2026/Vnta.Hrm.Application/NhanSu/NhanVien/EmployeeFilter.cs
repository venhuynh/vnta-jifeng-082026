namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public sealed record EmployeeFilter(
    string? SearchText,
    IReadOnlyList<int>? Statuses = null,
    int Take = 10000);
