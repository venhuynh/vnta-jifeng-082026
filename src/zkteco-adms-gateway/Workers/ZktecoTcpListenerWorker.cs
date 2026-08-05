using Vnta.AttendanceGateway.Configuration;
using Vnta.AttendanceGateway.Network;
using Microsoft.Extensions.Options;

namespace Vnta.AttendanceGateway.Workers;

public class ZktecoTcpListenerWorker(ZktecoTcpServerManager tcpServerManager, IOptions<AttendanceGatewayOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (options.Value.AutoStartTcpListener)
        {
            await tcpServerManager.StartListeningAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await tcpServerManager.StopListeningAsync(cancellationToken);
    }
}

