using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed class ApprovalEscalationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ApprovalEscalationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("ApprovalEscalation:Enabled", false))
        {
            logger.LogInformation("Approval escalation worker is disabled.");
            return;
        }

        var intervalMinutes = Math.Max(5, configuration.GetValue("ApprovalEscalation:IntervalMinutes", 30));
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IApprovalEscalationService>();
                var count = await service.EscalateOverdueApprovalsAsync(stoppingToken);
                if (count > 0)
                {
                    logger.LogInformation("Approval escalation worker processed {Count} approvals.", count);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Approval escalation worker failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
