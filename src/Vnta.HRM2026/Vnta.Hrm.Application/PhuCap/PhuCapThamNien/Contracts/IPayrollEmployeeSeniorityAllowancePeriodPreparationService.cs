namespace Vnta.Hrm.Application.PhuCap.PhuCapThamNien;

public interface IPayrollEmployeeSeniorityAllowancePeriodPreparationService
{
    Task PreparePeriodAsync(int year, int month, CancellationToken cancellationToken = default);
}
