using Hop.Api.DTOs;
using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using System.Text.Json;

namespace Hop.Api.Services;

public sealed class LineMessagingService(
    AppDbContext db,
    IConfiguration configuration,
    ILogger<LineMessagingService> logger) : ILineMessagingService
{
    public async Task NotifyLeaveRequestAsync(LeaveNotificationMessage message, CancellationToken cancellationToken = default)
    {
        var enabled = configuration.GetValue("Line:Enabled", false);
        db.LineDeliveryLogs.Add(new LineDeliveryLog
        {
            LeaveRequestId = message.LeaveRequestId,
            RecipientUserId = message.UserId,
            EventName = "LeaveRequestStatusChanged",
            Status = enabled ? "Queued" : "Disabled",
            Payload = JsonSerializer.Serialize(message),
            ResponseDetail = enabled
                ? "Queued for future LINE sender worker."
                : "LINE delivery is disabled for this phase.",
            AttemptCount = 0,
            NextRetryAt = enabled ? DateTime.UtcNow.AddMinutes(5) : null
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "LINE notification prepared: leave request {LeaveRequestId} status {Status}, enabled {Enabled}.",
            message.LeaveRequestId,
            message.Status,
            enabled);
    }
}
