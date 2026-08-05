namespace Vnta.Hrm.Application.NhanSu.NhanVien;

public static class EmployeeStatusCatalog
{
    public const int Probation = 1;
    public const int Official = 2;
    public const int Resigned = 5;

    public static IReadOnlyList<int> WorkingStatuses { get; } = [Probation, Official];
}
