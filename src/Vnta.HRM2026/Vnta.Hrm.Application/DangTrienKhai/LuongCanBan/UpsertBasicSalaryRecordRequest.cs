namespace Vnta.Hrm.Application.DangTrienKhai.LuongCanBan;

public sealed class UpsertBasicSalaryRecordRequest
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public decimal BasicSalary { get; set; }

    public decimal StandardWorkingDays { get; set; }

    public decimal DailySalary { get; set; }

    public decimal HourlySalary { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
