namespace Vnta.Hrm.Web.Client.Components.PhuCap.PhuCapDocHai;

/// <summary>View model used only by the hazard allowance manual-adjustment dialog.</summary>
public sealed class PhuCapDocHaiEditModel
{
    public Guid PayrollAllowanceSummaryRecordId { get; set; }
    public string EmployeeDisplay { get; set; } = string.Empty;
    public string PayrollPeriod { get; set; } = string.Empty;
    public decimal QualifiedWorkdayCount { get; set; }
    public decimal LateEarlyDeductionDays { get; set; }
    public decimal PayableWorkdayCount { get; set; }
    public decimal HazardAllowancePerDay { get; set; }
    public decimal HazardAllowanceAmount { get; set; }
    public bool IsEligibleDepartment { get; set; }
    public string? ExclusionReason { get; set; }
    public DateTime OriginalDetailUpdatedAtUtc { get; set; }
    public DateTime OriginalSummaryUpdatedAtUtc { get; set; }
}
