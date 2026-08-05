namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

public interface ILeaveHolidayAllowancePeriodPreparationService
{
    Task PreparePeriodAsync(int payrollYear, int payrollMonth, CancellationToken cancellationToken = default);
}
