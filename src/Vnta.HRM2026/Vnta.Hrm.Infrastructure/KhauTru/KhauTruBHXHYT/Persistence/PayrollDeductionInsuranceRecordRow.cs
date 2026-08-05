namespace Vnta.Hrm.Infrastructure.KhauTru.KhauTruBHXHYT;

public sealed class PayrollDeductionInsuranceRecordRow
{
    public Guid PayrollDeductionSummaryRecordId { get; set; }

    public decimal InsuranceSalaryBaseAmount { get; set; }

    public decimal SocialInsuranceRate { get; set; }

    public decimal HealthInsuranceRate { get; set; }

    public decimal UnemploymentInsuranceRate { get; set; }

    public decimal TotalInsuranceRate { get; set; }

    public decimal SocialInsuranceAmount { get; set; }

    public decimal HealthInsuranceAmount { get; set; }

    public decimal UnemploymentInsuranceAmount { get; set; }

    public decimal TotalDeductionAmount { get; set; }

    public bool IsParticipating { get; set; }

    public short ParticipationChangeType { get; set; }

    public DateOnly? EffectiveDate { get; set; }

    public bool IsLocked { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
