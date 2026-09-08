using System.Text.Json;
using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed record DomainEventEnvelope(string EventType, string Scope, string AggregateType, Guid AggregateId, Guid? ActorUserId, string CorrelationId, object Payload, IReadOnlyList<Guid> RecipientUserIds);
public interface IDomainEventPublisher { Task PublishAsync(DomainEventEnvelope envelope, CancellationToken ct); }

public sealed class DomainEventPublisher(AppDbContext db, INotificationRecipientResolver recipientResolver) : IDomainEventPublisher
{
    public async Task PublishAsync(DomainEventEnvelope e, CancellationToken ct)
    {
        var eventId = Guid.NewGuid(); var payload = JsonSerializer.Serialize(e.Payload);
        db.DomainEvents.Add(new DomainEventRecord { EventId = eventId, EventType = e.EventType, Scope = e.Scope, AggregateType = e.AggregateType, AggregateId = e.AggregateId, ActorUserId = e.ActorUserId, CorrelationId = e.CorrelationId, Payload = payload });
        var outbox = new OutboxMessage { EventId = eventId, EventType = e.EventType, Scope = e.Scope, Payload = payload };
        var recipientIds = e.RecipientUserIds.Count > 0 ? e.RecipientUserIds : await recipientResolver.ResolveAsync(e.EventType, e.AggregateId, ct);
        foreach (var userId in recipientIds.Distinct())
        {
            outbox.Deliveries.Add(new NotificationDelivery { RecipientUserId = userId, Channel = "IN_APP", IdempotencyKey = $"{eventId}:{userId}:IN_APP" });
            outbox.Deliveries.Add(new NotificationDelivery { RecipientUserId = userId, Channel = "LINE", IdempotencyKey = $"{eventId}:{userId}:LINE" });
        }
        db.OutboxMessages.Add(outbox);
    }
}

public sealed class FleetNotificationTemplateService
{
    private static readonly Dictionary<string, (string Title, string Message)> Templates = new()
    {
        ["Fleet.RequestSubmitted"] = ("มีคำขอใช้รถใหม่", "มีคำขอใช้รถรอจัดรถและคนขับ"), ["Fleet.Assigned"] = ("จัดรถและคนขับแล้ว", "คำขอพร้อมให้หัวหน้าฝ่ายบริหารตรวจสอบ"), ["Fleet.AdminReviewApproved"] = ("คำขอผ่านการตรวจสอบ", "คำขอรอผู้อำนวยการอนุมัติ"), ["Fleet.DirectorApproved"] = ("คำขอใช้รถได้รับอนุมัติ", "กรุณาตรวจสอบรายละเอียดงานและตอบรับ"), ["Fleet.RequestReturned"] = ("คำขอใช้รถถูกส่งกลับ", "กรุณาตรวจสอบเหตุผลและแก้ไข"), ["Fleet.RequestRejected"] = ("คำขอใช้รถไม่ผ่านการพิจารณา", "กรุณาตรวจสอบเหตุผล"), ["Fleet.DriverAccepted"] = ("คนขับตอบรับงานแล้ว", "งานพร้อมเดินทาง"), ["Fleet.DriverDeclined"] = ("คนขับปฏิเสธงาน", "กรุณาจัดคนขับใหม่"), ["Fleet.TripStarted"] = ("เริ่มภารกิจแล้ว", "คนขับเริ่มภารกิจแล้ว"), ["Fleet.TripCompleted"] = ("ภารกิจเสร็จสิ้น", "คนขับปิดงานเรียบร้อยแล้ว"), ["Fleet.TripAborted"] = ("ภารกิจถูกยุติ", "ผู้ดูแลยุติภารกิจ กรุณาตรวจสอบเหตุผล"), ["Fleet.TripMileageOverridden"] = ("แก้ไขเลขไมล์ภารกิจ", "ผู้ดูแลแก้ไขเลขไมล์พร้อมบันทึกเหตุผล"), ["Fleet.AssignmentReplaced"] = ("มีการเปลี่ยนรถหรือคนขับ", "กรุณาตรวจสอบรายละเอียด assignment ล่าสุด")
    };
    public (string Title, string Message) Resolve(string eventType) => Templates.TryGetValue(eventType, out var value) ? value : ("แจ้งเตือนระบบรถ", "มีการเปลี่ยนแปลงรายการใช้รถ");
}

public sealed class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatch(stoppingToken); } catch (Exception ex) { logger.LogError(ex, "Outbox processing failed."); }
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
    private async Task ProcessBatch(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var line = scope.ServiceProvider.GetRequiredService<ILineMessagingService>(); var templates = scope.ServiceProvider.GetRequiredService<FleetNotificationTemplateService>();
        var messages = await db.OutboxMessages.Include(x => x.Deliveries).Where(x => (x.Status == "PENDING" || x.Status == "RETRY") && x.AvailableAt <= DateTime.UtcNow).OrderBy(x => x.CreatedAt).Take(20).ToListAsync(ct);
        foreach (var message in messages)
        {
            foreach (var delivery in message.Deliveries.Where(x => x.Status is "PENDING" or "RETRY"))
            {
                try
                {
                    var text = templates.Resolve(message.EventType);
                    if (delivery.Channel == "IN_APP")
                    {
                        var referenceId = delivery.Id.ToString();
                        if (!await db.Notifications.AnyAsync(x => x.ReferenceEntity == "FleetOutboxDelivery" && x.ReferenceId == referenceId, ct))
                            db.Notifications.Add(new Notification { UserId = delivery.RecipientUserId, Category = "Fleet", NotificationType = message.EventType, Title = text.Title, Message = text.Message, ReferenceEntity = "FleetOutboxDelivery", ReferenceId = referenceId, ActionUrl = "/fleet/requests" });
                    }
                    else await line.NotifyUserAsync(delivery.RecipientUserId, message.EventType, text.Message, null, ct);
                    delivery.Status = "PROCESSED"; delivery.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex) { delivery.AttemptCount++; delivery.LastError = ex.Message[..Math.Min(ex.Message.Length, 1000)]; delivery.Status = delivery.AttemptCount >= 5 ? "FAILED" : "RETRY"; }
                if (delivery.Status is "FAILED" or "RETRY") logger.LogWarning("Fleet notification delivery issue. EventType={EventType} OutboxMessageId={OutboxMessageId} DeliveryChannel={DeliveryChannel} RetryCount={RetryCount} ErrorCode={ErrorCode}", message.EventType, message.Id, delivery.Channel, delivery.AttemptCount, delivery.Status);
            }
            message.AttemptCount++; message.ProcessedAt = message.Deliveries.All(x => x.Status is "PROCESSED" or "FAILED") ? DateTime.UtcNow : null; message.Status = message.Deliveries.Any(x => x.Status == "RETRY") ? "RETRY" : message.Deliveries.Any(x => x.Status == "FAILED") ? "FAILED" : "PROCESSED"; message.AvailableAt = DateTime.UtcNow.AddMinutes(Math.Min(message.AttemptCount, 5)); message.ConcurrencyToken = Guid.NewGuid();
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Fleet outbox processed. EventType={EventType} OutboxMessageId={OutboxMessageId} Status={OutboxStatus} RetryCount={RetryCount}", message.EventType, message.Id, message.Status, message.AttemptCount);
        }
    }
}
