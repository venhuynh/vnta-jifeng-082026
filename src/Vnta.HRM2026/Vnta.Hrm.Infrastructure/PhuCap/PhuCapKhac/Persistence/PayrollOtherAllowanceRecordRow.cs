namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapKhac;

public sealed class PayrollOtherAllowanceRecordRow
{
    public Guid Id { get; set; }
    public Guid PayrollAllowanceSummaryRecordId { get; set; }
    public string AllowanceName { get; set; } = string.Empty;
    public bool IsFixedAmount { get; set; }
    public decimal AllowanceAmount { get; set; }
    public string? Note { get; set; }
    public bool IsLocked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
}
