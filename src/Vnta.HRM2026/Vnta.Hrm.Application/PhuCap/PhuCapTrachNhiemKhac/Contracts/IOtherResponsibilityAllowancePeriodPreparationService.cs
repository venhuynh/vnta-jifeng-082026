namespace Vnta.Hrm.Application.PhuCap.PhuCapTrachNhiemKhac.Contracts;

/// <summary>Capability that materializes the detail snapshot for a payroll period.</summary>
public interface IOtherResponsibilityAllowancePeriodPreparationService
{
    Task PreparePeriodAsync(
        int year,
        int month,
        string? requestedBy,
        CancellationToken cancellationToken = default);
}
