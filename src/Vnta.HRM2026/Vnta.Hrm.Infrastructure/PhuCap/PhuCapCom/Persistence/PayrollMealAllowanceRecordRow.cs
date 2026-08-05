namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapCom;

public sealed class PayrollMealAllowanceRecordRow
{
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    public int QualifiedMealDays { get; set; }

    public int Overtime1900Days { get; set; }

    public decimal MealAllowancePerQualifiedDay { get; set; }

    public decimal MealAllowanceAmount { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    public string? RuleVersion { get; set; }

    public string? Note { get; set; }

    public bool IsLocked { get; set; }

    public DateTime CalculatedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
