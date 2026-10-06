using Hop.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed class FleetLineGroupDeliveryWorker(
    IServiceScopeFactory scopes,
    IOptions<LineGroupNotificationsOptions> options,
    ILogger<FleetLineGroupDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled || !options.Value.WorkerEnabled)
        {
            logger.LogInformation("Fleet LINE group delivery worker is disabled.");
            return;
        }
        logger.LogInformation("Fleet LINE group delivery worker started. PollIntervalSeconds={PollIntervalSeconds}", options.Value.PollIntervalSeconds);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Clamp(options.Value.PollIntervalSeconds, 2, 300)));
        try
        {
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<FleetLineGroupDeliveryService>();
                    var projected = await service.ProjectMissingEventsAsync(stoppingToken);
                    var discovered = await service.DiscoverAsync(stoppingToken);
                    var processed = await service.ProcessAsync(stoppingToken);
                    if (projected > 0 || discovered > 0 || processed > 0)
                        logger.LogInformation("Fleet LINE group delivery batch. Projected={Projected} Discovered={Discovered} Processed={Processed}", projected, discovered, processed);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogError(ex, "Fleet LINE group delivery worker batch failed; USER delivery is unaffected."); }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
