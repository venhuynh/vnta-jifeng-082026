namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

public interface ILeaveHolidayAllowanceRecalculationService
{
    Task<RecalculateLeaveHolidayAllowanceResult> RecalculateAsync(RecalculateLeaveHolidayAllowanceRequest request, CancellationToken cancellationToken = default);
}
