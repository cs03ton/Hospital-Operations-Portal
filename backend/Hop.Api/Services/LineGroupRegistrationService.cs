using System.Net.Http.Headers;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Hop.Api.Services;

public sealed class LineGroupRegistrationService(
    AppDbContext db,
    LineConfigurationResolver lineConfiguration,
    IOptions<LineGroupNotificationsOptions> groupOptions,
    HttpClient httpClient,
    ILogger<LineGroupRegistrationService> logger)
{
    public bool Enabled => groupOptions.Value.Enabled;

    public async Task EnqueueAsync(JsonElement lineEvent, CancellationToken ct)
    {
        if (!Enabled) return;
        if (!lineEvent.TryGetProperty("source", out var source) ||
            !source.TryGetProperty("type", out var sourceTypeProperty) ||
            !string.Equals(sourceTypeProperty.GetString(), "group", StringComparison.OrdinalIgnoreCase) ||
            !source.TryGetProperty("groupId", out var groupIdProperty) ||
            string.IsNullOrWhiteSpace(groupIdProperty.GetString())) return;

        var payload = lineEvent.GetRawText();
        var webhookEventId = lineEvent.TryGetProperty("webhookEventId", out var idProperty) && !string.IsNullOrWhiteSpace(idProperty.GetString())
            ? idProperty.GetString()!
            : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload)));
        if (await db.LineWebhookInbox.AnyAsync(x => x.WebhookEventId == webhookEventId, ct)) return;

        var eventType = lineEvent.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() ?? "unknown" : "unknown";
        db.LineWebhookInbox.Add(new LineWebhookInbox
        {
            WebhookEventId = webhookEventId,
            EventType = eventType,
            SourceType = "group",
            SourceGroupId = groupIdProperty.GetString()!.Trim(),
            Payload = payload
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> ProcessPendingAsync(CancellationToken ct)
    {
        if (!Enabled) return 0;
        var now = DateTime.UtcNow;
        var items = await db.LineWebhookInbox.Where(x => x.Status == "Pending" && x.AvailableAt <= now)
            .OrderBy(x => x.CreatedAt).Take(20).ToListAsync(ct);
        foreach (var item in items)
        {
            try
            {
                await ProcessAsync(item, ct);
                item.Status = "Processed";
                item.ProcessedAt = DateTime.UtcNow;
                item.LastError = null;
            }
            catch (Exception ex)
            {
                item.AttemptCount++;
                item.LastError = Sanitize(ex.Message);
                item.Status = item.AttemptCount >= 5 ? "Failed" : "Pending";
                item.AvailableAt = DateTime.UtcNow.AddMinutes(Math.Min(30, 1 << Math.Min(item.AttemptCount, 4)));
                logger.LogWarning(ex, "LINE group webhook processing failed. WebhookEventId={WebhookEventId}", item.WebhookEventId);
            }
        }
        if (items.Count > 0) await db.SaveChangesAsync(ct);
        return items.Count;
    }

    private async Task ProcessAsync(LineWebhookInbox item, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(item.SourceGroupId)) return;
        using var document = JsonDocument.Parse(item.Payload);
        var root = document.RootElement;
        if (item.EventType == "leave")
        {
            var existing = await db.LineGroupDestinations.SingleOrDefaultAsync(x => x.LineGroupId == item.SourceGroupId, ct);
            if (existing is not null)
            {
                existing.Status = LineGroupDestinationStatuses.Disabled;
                existing.AttentionRequired = true;
                existing.AttentionReason = "LINE OA ถูกนำออกจากกลุ่ม";
                existing.DisabledAt = DateTime.UtcNow;
                existing.ConcurrencyToken = Guid.NewGuid();
            }
            return;
        }

        if (item.EventType == "message" && !IsRegistrationCommand(root)) return;
        if (item.EventType is not ("message" or "join")) return;

        var destination = await db.LineGroupDestinations.Include(x => x.EventSubscriptions)
            .SingleOrDefaultAsync(x => x.LineGroupId == item.SourceGroupId, ct);
        if (destination is null)
        {
            destination = new LineGroupDestination { LineGroupId = item.SourceGroupId, DisplayName = await GetGroupNameAsync(item.SourceGroupId, ct) };
            foreach (var subscription in FleetLineGroupEvents.Defaults)
                destination.EventSubscriptions.Add(new LineGroupEventSubscription { EventType = subscription.Key, IsEnabled = subscription.Value });
            db.LineGroupDestinations.Add(destination);
        }
        else
        {
            destination.LastDetectedAt = DateTime.UtcNow;
            if (destination.Status == LineGroupDestinationStatuses.Disabled)
            {
                destination.Status = LineGroupDestinationStatuses.Pending;
                destination.AttentionRequired = false;
                destination.AttentionReason = null;
                destination.DisabledAt = null;
                destination.DisabledByUserId = null;
            }
            destination.ConcurrencyToken = Guid.NewGuid();
        }
    }

    private async Task<string> GetGroupNameAsync(string groupId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(lineConfiguration.AccessToken)) return "กลุ่ม LINE ที่ตรวจพบ";
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.line.me/v2/bot/group/{Uri.EscapeDataString(groupId)}/summary");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", lineConfiguration.AccessToken);
            using var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return "กลุ่ม LINE ที่ตรวจพบ";
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return json.RootElement.TryGetProperty("groupName", out var name) && !string.IsNullOrWhiteSpace(name.GetString())
                ? name.GetString()!.Trim() : "กลุ่ม LINE ที่ตรวจพบ";
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Unable to resolve LINE group name. GroupId={GroupId}", Mask(item: groupId));
            return "กลุ่ม LINE ที่ตรวจพบ";
        }
    }

    private static bool IsRegistrationCommand(JsonElement root) =>
        root.TryGetProperty("message", out var message) &&
        message.TryGetProperty("type", out var type) && type.GetString() == "text" &&
        message.TryGetProperty("text", out var text) &&
        string.Equals(text.GetString()?.Trim(), "ลงทะเบียนกลุ่ม HOP", StringComparison.OrdinalIgnoreCase);

    public static string Mask(string item) => item.Length <= 10 ? "***" : $"{item[..5]}...{item[^4..]}";
    private static string Sanitize(string value)
    {
        var sanitized = value.Replace("Bearer ", string.Empty, StringComparison.OrdinalIgnoreCase);
        return sanitized[..Math.Min(sanitized.Length, 900)];
    }
}
