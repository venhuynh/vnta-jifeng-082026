namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapChuyenCan;

/// <summary>Feature-owned one-to-one detail snapshot keyed by the shared payroll allowance summary.</summary>
public sealed class PayrollAttendanceAllowanceRecordRow
{
    public Guid PayrollAllowanceSummaryRecordId { get; set; }
    public decimal StandardAllowanceAmount { get; set; }
    public decimal StandardWorkdayCount { get; set; }
    public decimal ActualWorkdayCount { get; set; }
    public decimal AdministrativeWorkdayCount { get; set; }
    public decimal LateEarlyDeductionDays { get; set; }
    public decimal AttendanceRate { get; set; }
    public decimal AllowanceAmount { get; set; }
    public string? AppliedRuleKey { get; set; }
    public string? AttendanceClass { get; set; }
    public decimal? CtlWorkdayCount { get; set; }
    public int? LateEarlyMinutes { get; set; }
    public decimal? Kqcc { get; set; }
    public bool HasKpViolation { get; set; }
    public string? Note { get; set; }
    public bool IsLocked { get; set; }
    public DateTime? RefreshedAtUtc { get; set; }
    public string? RefreshedBy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}
