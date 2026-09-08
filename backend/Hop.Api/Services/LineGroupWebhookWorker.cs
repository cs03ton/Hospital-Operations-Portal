namespace Hop.Api.Services;

public sealed class LineGroupWebhookWorker(IServiceScopeFactory scopes, ILogger<LineGroupWebhookWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        try
        {
            do
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<LineGroupRegistrationService>();
                    if (!service.Enabled)
                    {
                        logger.LogInformation("LINE group webhook worker is disabled.");
                        return;
                    }
                    await service.ProcessPendingAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogWarning(ex, "LINE group webhook worker failed."); }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
