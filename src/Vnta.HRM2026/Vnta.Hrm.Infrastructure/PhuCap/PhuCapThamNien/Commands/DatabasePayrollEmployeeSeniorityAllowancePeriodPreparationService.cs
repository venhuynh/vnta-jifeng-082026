using Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Persistence;

namespace Vnta.Hrm.Infrastructure.PhuCap.PhuCapThamNien.Commands;

public sealed class DatabasePayrollEmployeeSeniorityAllowancePeriodPreparationService(SeniorityAllowancePeriodWriter writer)
    : IPayrollEmployeeSeniorityAllowancePeriodPreparationService
{
    public Task PreparePeriodAsync(int year, int month, CancellationToken cancellationToken = default) =>
        writer.PrepareAsync(year, month, cancellationToken);
}
