using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.QuanTri.GiamSatAdms;

public sealed class AdmsMonitorReadService(AdmsMonitorMemoryStore monitorStore) : IAdmsMonitorReadService
{
    public Task<AdmsMonitorSnapshotDto> GetSnapshotAsync(
        int activityLimit,
        int rawLimit)
        => Task.FromResult(monitorStore.GetSnapshot(activityLimit, rawLimit));
}
