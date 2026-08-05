namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionUnionFeeRecordRow : IPayrollDeductionAmountRecord
{
    public Guid PayrollDeductionSummaryRecordId { get; set; }

    public decimal DeductionAmount { get; set; }

    public bool IsLocked { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
