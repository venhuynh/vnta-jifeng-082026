namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

public sealed class UpsertPayrollInsuranceDeductionRequest
{
    public Guid Id { get; set; }

    public Guid? PayrollDeductionSummaryRecordId { get; set; }

    public Guid EmployeeId { get; set; }

    public int PayrollMonth { get; set; }

    public int PayrollYear { get; set; }

    public decimal InsuranceSalaryBaseAmount { get; set; }

    public decimal SocialInsuranceRate { get; set; }

    public decimal HealthInsuranceRate { get; set; }

    public decimal UnemploymentInsuranceRate { get; set; }

    public bool IsParticipating { get; set; } = true;

    public short ParticipationChangeType { get; set; }

    public DateOnly? EffectiveDate { get; set; }

    public bool IsLocked { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
