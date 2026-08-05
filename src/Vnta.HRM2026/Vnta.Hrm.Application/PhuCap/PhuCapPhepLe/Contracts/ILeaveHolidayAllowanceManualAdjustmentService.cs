namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;
using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Queries;

public interface ILeaveHolidayAllowanceManualAdjustmentService
{
    Task<LeaveHolidayAllowanceListItemDto> UpdateManualValuesAsync(UpdateLeaveHolidayAllowanceManualValuesRequest request, CancellationToken cancellationToken = default);
}
