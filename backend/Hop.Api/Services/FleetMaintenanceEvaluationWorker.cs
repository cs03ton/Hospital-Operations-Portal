using Hop.Api.Configuration;
using Hop.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed class FleetMaintenanceEvaluationWorker(IServiceScopeFactory scopes, IOptions<FleetOperationsOptions> options, ILogger<FleetMaintenanceEvaluationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Evaluate(stoppingToken); } catch (Exception ex) { logger.LogError(ex, "Fleet maintenance evaluation failed."); }
            await Task.Delay(options.Value.Maintenance.EvaluationInterval < TimeSpan.FromMinutes(5) ? TimeSpan.FromMinutes(5) : options.Value.Maintenance.EvaluationInterval, stoppingToken);
        }
    }
    private async Task Evaluate(CancellationToken ct)
    {
        using var scope = scopes.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>(); var now = DateTime.UtcNow; var reminderEnd = now.AddDays(options.Value.Maintenance.DefaultReminderDays);
        var schedules = await db.FleetVehicleMaintenanceSchedules.AsNoTracking().Include(x => x.Vehicle).Where(x => x.IsActive && x.Status == "ACTIVE" && ((x.DueDate != null && x.DueDate < reminderEnd) || (x.DueMileage != null && x.DueMileage <= x.Vehicle!.CurrentMileage + options.Value.Maintenance.DefaultReminderMileage))).ToListAsync(ct);
        foreach (var item in schedules)
        {
            var overdue = item.DueDate < now || item.DueMileage < item.Vehicle!.CurrentMileage; var eventType = overdue ? "FleetMaintenance.Overdue" : "FleetMaintenance.DueSoon";
            if (await db.DomainEvents.AnyAsync(x => x.EventType == eventType && x.AggregateId == item.Id, ct)) continue;
            await publisher.PublishAsync(new(eventType, "FLEET", "FleetMaintenance", item.Id, null, "maintenance-evaluator", new { item.VehicleId, item.DueDate, item.DueMileage }, []), ct);
        }
        await db.SaveChangesAsync(ct);
    }
}
