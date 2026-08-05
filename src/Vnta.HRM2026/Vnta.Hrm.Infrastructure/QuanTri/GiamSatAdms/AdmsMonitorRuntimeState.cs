using Microsoft.Extensions.Logging;
using Vnta.Hrm.Application.Integrations.AttendanceGateway;

namespace Vnta.Hrm.Infrastructure.QuanTri.GiamSatAdms;

public sealed class AdmsMonitorRuntimeState(
    AdmsMonitorMemoryStore monitorStore,
    ILogger<AdmsMonitorRuntimeState> logger)
    : IAdmsMonitorRuntimeState
{
    private int activeSessionCount;

    public bool HasActiveSessions => Volatile.Read(ref activeSessionCount) > 0;

    public int ActivateSession()
    {
        var currentCount = Interlocked.Increment(ref activeSessionCount);
        if(currentCount == 1) {
            monitorStore.Reset();
            logger.LogInformation(
                "Activated ADMS monitor runtime. HRM will buffer and rebroadcast realtime data only while at least one viewer is connected.");
        }

        return currentCount;
    }

    public int DeactivateSession()
    {
        var currentCount = Interlocked.Decrement(ref activeSessionCount);
        if(currentCount <= 0) {
            Interlocked.Exchange(ref activeSessionCount, 0);
            monitorStore.Reset();

            logger.LogInformation(
                "Released ADMS monitor runtime because the last viewer left the screen. HRM cleared all in-memory monitor state.");

            return 0;
        }

        return currentCount;
    }
}
