using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed class LineRetryWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<LineRetryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("LineRetry:Enabled", true))
        {
            logger.LogInformation("LINE retry worker is disabled.");
            return;
        }

        var intervalMinutes = Math.Max(1, configuration.GetValue("LineRetry:IntervalMinutes", 5));
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        try
        {
            do
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var lineService = scope.ServiceProvider.GetRequiredService<ILineMessagingService>();
                    var count = await lineService.RetryPendingDeliveriesAsync(stoppingToken);
                    if (count > 0)
                    {
                        logger.LogInformation("LINE retry worker processed {Count} delivery logs.", count);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "LINE retry worker failed.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("LINE retry worker is stopping.");
        }
    }
}
