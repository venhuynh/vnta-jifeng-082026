namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapTrachNhiemKhac;

public sealed class PayrollAllowanceOtherResponsibilityRecordRow
{
    public Guid PayrollAllowanceSummaryRecordId { get; set; }

    public decimal AllowanceWorkdayCount { get; set; }

    public decimal StandardResponsibilityAllowanceAmount { get; set; }

    public decimal ActualResponsibilityAllowanceAmount { get; set; }

    public string? Note { get; set; }

    public bool IsLocked { get; set; }

    public DateTime? RefreshedAtUtc { get; set; }

    public string? RefreshedBy { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
