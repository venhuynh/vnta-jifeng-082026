namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Commands;

/// <summary>
/// Command tính lại snapshot Phụ cấp trách nhiệm khác theo kỳ lương.
/// </summary>
public interface IOtherResponsibilityAllowanceRecalculationService
{
    Task<RecalculateOtherResponsibilityAllowanceResult> RecalculateAsync(
        RecalculateOtherResponsibilityAllowanceRequest request,
        string? requestedBy = null,
        CancellationToken cancellationToken = default);
}
