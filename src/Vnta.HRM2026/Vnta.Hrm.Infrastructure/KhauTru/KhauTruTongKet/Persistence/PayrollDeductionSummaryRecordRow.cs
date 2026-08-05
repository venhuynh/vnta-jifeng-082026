namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruTongHop;

public sealed class PayrollDeductionSummaryRecordRow
{
    public Guid Id { get; set; }

    public Guid EmployeeId { get; set; }

    public short PayrollMonth { get; set; }

    public short PayrollYear { get; set; }

    public decimal SocialInsuranceDeductionAmount { get; set; }

    public decimal PersonalIncomeTaxDeductionAmount { get; set; }

    public decimal UnionFeeDeductionAmount { get; set; }

    public decimal AdvanceDeductionAmount { get; set; }

    public decimal OtherDeductionAmount { get; set; }

    public bool IsLocked { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAtUtc { get; set; }

    public string? UpdatedBy { get; set; }
}
