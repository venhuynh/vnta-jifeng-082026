namespace Vnta.Hrm.Application.KhauTru.KhauTruBHXHYT;

/// <summary>
/// Các giá trị được phép điều chỉnh thủ công trên một dòng khấu trừ BHXH-YT.
/// Danh tính nhân viên, kỳ lương, trạng thái khóa, audit và các tổng tính toán
/// luôn được xác định lại ở server.
/// </summary>
public sealed record UpdatePayrollInsuranceDeductionManualValuesRequest(
    Guid PayrollDeductionSummaryRecordId,
    decimal InsuranceSalaryBaseAmount,
    decimal SocialInsuranceRate,
    decimal HealthInsuranceRate,
    decimal UnemploymentInsuranceRate,
    bool IsParticipating,
    short ParticipationChangeType,
    DateOnly? EffectiveDate,
    DateTime OriginalUpdatedAtUtc);
