using Microsoft.Extensions.Options;
using Vnta.Hrm.Application.PhuCap.PhuCapDocHai;

namespace Vnta.Hrm.Web.HostedServices;

public sealed class HazardAllowanceExportJobWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<HazardAllowanceExportJobOptions> optionsAccessor,
    ILogger<HazardAllowanceExportJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IHazardAllowanceExportJobProcessor>();
                var processed = await service.ProcessNextAsync(stoppingToken);
                if(!processed)
                {
                    await service.DeleteExpiredAsync(stoppingToken);
                    var seconds = Math.Clamp(optionsAccessor.Value.PollIntervalSeconds, 1, 60);
                    await Task.Delay(TimeSpan.FromSeconds(seconds), stoppingToken);
                }
            }
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "Hazard allowance export worker iteration failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
