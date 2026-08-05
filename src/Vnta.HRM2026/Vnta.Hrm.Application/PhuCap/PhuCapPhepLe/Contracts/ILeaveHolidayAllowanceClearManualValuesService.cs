namespace Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Contracts;

using Vnta.Hrm.Application.PhuCap.PhuCapPhepLe.Commands;

/// <summary>
/// Clears the manually entered values for a leave/holiday allowance period.
/// </summary>
public interface ILeaveHolidayAllowanceClearManualValuesService
{
    Task<ClearLeaveHolidayAllowanceManualValuesResult> ClearManualValuesAsync(ClearLeaveHolidayAllowanceManualValuesRequest request, CancellationToken cancellationToken = default);
}
