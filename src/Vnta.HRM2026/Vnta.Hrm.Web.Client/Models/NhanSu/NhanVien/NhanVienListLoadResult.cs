using Vnta.Hrm.Web.Client.Models.Employees;

namespace Vnta.Hrm.Web.Client.Models.NhanSu.NhanVien;

public sealed record NhanVienListLoadResult(
    IReadOnlyList<EmployeeRecord> Rows,
    int TotalCount);
