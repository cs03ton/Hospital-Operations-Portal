using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public sealed class AnnouncementNotificationDispatcher(
    AppDbContext db,
    IAnnouncementAudienceResolver audienceResolver,
    ILineMessagingService lineMessagingService,
    LineConfigurationResolver lineConfiguration,
    IAuditLogService auditLogService,
    ILogger<AnnouncementNotificationDispatcher> logger) : IAnnouncementNotificationDispatcher
{
    public async Task<AnnouncementNotificationPreviewResponse> PreviewAsync(
        Announcement announcement,
        bool? notifyInApp = null,
        bool? notifyViaLine = null,
        CancellationToken cancellationToken = default)
    {
        var inApp = notifyInApp ?? announcement.NotifyInApp;
        var line = notifyViaLine ?? announcement.NotifyViaLine;
        var audience = await audienceResolver.ResolveAsync(announcement, cancellationToken);
        var warnings = new List<string>();

        if (!inApp && !line)
        {
            warnings.Add("ประกาศจะแสดงในศูนย์ประกาศเท่านั้น และไม่สร้างรายการแจ้งเตือน");
        }

        if (line && audience.LineBoundUsers == 0)
        {
            warnings.Add("ไม่พบผู้รับที่เชื่อมต่อ LINE");
        }
        else if (line && audience.LineUnboundUsers > 0)
        {
            warnings.Add($"มีผู้รับ {audience.LineUnboundUsers:N0} รายที่ยังไม่ได้เชื่อมต่อ LINE ระบบจะส่งเฉพาะ Notification Bell ให้ตามช่องทางที่เลือก");
        }

        if (audience.InactiveUsers > 0)
        {
            warnings.Add($"ตัดผู้ใช้ที่ปิดใช้งานออก {audience.InactiveUsers:N0} ราย");
        }

        return new AnnouncementNotificationPreviewResponse(
            announcement.Id,
            inApp,
            line,
            audience.TotalMatchedUsers,
            inApp ? audience.ActiveUsers : 0,
            line ? audience.LineBoundUsers : 0,
            line ? audience.LineUnboundUsers : 0,
            audience.InactiveUsers,
            (inApp ? audience.ActiveUsers : 0) + (line ? audience.LineBoundUsers : 0),
            warnings);
    }

    public async Task DispatchAsync(
        Announcement announcement,
        Guid? actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (!announcement.NotifyInApp && !announcement.NotifyViaLine)
        {
            announcement.NotificationDispatchStatus = "Skipped";
            announcement.NotificationDispatchError = null;
            await db.SaveChangesAsync(cancellationToken);
            await auditLogService.WriteAsync(actorUserId, "Announcement.NotificationSkipped", "Announcement", announcement.Id.ToString(), announcement.Title, "Success");
            return;
        }

        var now = DateTime.UtcNow;
        var audience = await audienceResolver.ResolveAsync(announcement, cancellationToken);
        var existingKeys = await db.AnnouncementNotificationDeliveries
            .Where(item => item.AnnouncementId == announcement.Id)
            .Select(item => item.IdempotencyKey)
            .ToListAsync(cancellationToken);
        var existing = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var inAppCreated = 0;
        var lineQueued = 0;
        foreach (var user in audience.Users)
        {
            if (announcement.NotifyInApp)
            {
                var key = BuildIdempotencyKey(announcement, AnnouncementNotificationChannels.InApp, user.Id);
                if (!existing.Contains(key))
                {
                    var notification = new Notification
                    {
                        UserId = user.Id,
                        Channel = "InApp",
                        Category = "Announcement",
                        NotificationType = announcement.RequiresAcknowledgement ? "ActionRequired" : "Information",
                        Priority = ToNotificationPriority(announcement.Priority),
                        Title = $"ประกาศใหม่: {announcement.Title}",
                        Message = announcement.Summary,
                        ActionUrl = $"/announcements/{announcement.Id}",
                        ReferenceEntity = "Announcement",
                        ReferenceId = announcement.Id.ToString(),
                        ExpiresAt = announcement.ExpiresAt,
                        IsRead = false,
                        CreatedAt = now
                    };
                    db.Notifications.Add(notification);
                    db.AnnouncementNotificationDeliveries.Add(new AnnouncementNotificationDelivery
                    {
                        AnnouncementId = announcement.Id,
                        UserId = user.Id,
                        Channel = AnnouncementNotificationChannels.InApp,
                        Status = AnnouncementNotificationDeliveryStatuses.Sent,
                        IdempotencyKey = key,
                        QueuedAt = now,
                        SentAt = now,
                        Notification = notification,
                        CreatedAt = now
                    });
                    existing.Add(key);
                    inAppCreated += 1;
                }
            }

            if (announcement.NotifyViaLine && !string.IsNullOrWhiteSpace(user.LineUserId))
            {
                var key = BuildIdempotencyKey(announcement, AnnouncementNotificationChannels.Line, user.Id);
                if (!existing.Contains(key))
                {
                    var lineLog = await lineMessagingService.NotifyUserPayloadAsync(
                        user.Id,
                        "Announcement.Published",
                        BuildLineFlexPayload(announcement, lineConfiguration.PublicAppUrl),
                        null,
                        cancellationToken);
                    db.AnnouncementNotificationDeliveries.Add(new AnnouncementNotificationDelivery
                    {
                        AnnouncementId = announcement.Id,
                        UserId = user.Id,
                        Channel = AnnouncementNotificationChannels.Line,
                        Status = ToDeliveryStatus(lineLog.Status),
                        IdempotencyKey = key,
                        QueuedAt = now,
                        SentAt = lineLog.SentAt,
                        FailedAt = lineLog.Status == "Failed" ? DateTime.UtcNow : null,
                        RetryCount = lineLog.AttemptCount,
                        LastErrorMessageSanitized = lineLog.Status == "Failed" ? SanitizeLineError(lineLog.ResponseDetail) : null,
                        LineQueue = lineLog,
                        CreatedAt = now
                    });
                    existing.Add(key);
                    lineQueued += 1;
                }
            }
        }

        if (announcement.NotifyInApp)
        {
            announcement.NotificationSentAt ??= now;
        }

        if (announcement.NotifyViaLine)
        {
            announcement.LineNotificationQueuedAt ??= now;
        }

        announcement.NotificationDispatchError = null;
        announcement.NotificationDispatchStatus = await ResolveDispatchStatusAsync(announcement.Id, inAppCreated, lineQueued, cancellationToken);
        announcement.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
        await auditLogService.WriteAsync(
            actorUserId,
            "Announcement.NotificationDispatched",
            "Announcement",
            announcement.Id.ToString(),
            $"InApp={inAppCreated}; LineQueued={lineQueued}; Audience={audience.ActiveUsers}",
            "Success");

        logger.LogInformation(
            "Announcement notification dispatched. AnnouncementId={AnnouncementId} InApp={InAppCreated} LineQueued={LineQueued} Audience={Audience}",
            announcement.Id,
            inAppCreated,
            lineQueued,
            audience.ActiveUsers);
    }

    private static string BuildIdempotencyKey(Announcement announcement, string channel, Guid userId)
    {
        var version = Math.Max(1, announcement.NotificationConfigVersion);
        return $"Announcement:{channel}:{announcement.Id}:{userId}:v{version}";
    }

    private async Task<string> ResolveDispatchStatusAsync(Guid announcementId, int inAppCreated, int lineQueued, CancellationToken cancellationToken)
    {
        if (lineQueued > 0)
        {
            return "Queued";
        }

        if (inAppCreated > 0)
        {
            return "Sent";
        }

        var existingLineStatuses = await db.AnnouncementNotificationDeliveries
            .AsNoTracking()
            .Where(item => item.AnnouncementId == announcementId && item.Channel == AnnouncementNotificationChannels.Line)
            .Select(item => item.LineQueue != null ? item.LineQueue.Status : item.Status)
            .ToListAsync(cancellationToken);
        if (existingLineStatuses.Any(status => status == "Queued" || status == "Failed"))
        {
            return "Queued";
        }

        if (existingLineStatuses.Any(status => status == "Sent"))
        {
            return "Sent";
        }

        var hasInApp = await db.AnnouncementNotificationDeliveries
            .AsNoTracking()
            .AnyAsync(item => item.AnnouncementId == announcementId && item.Channel == AnnouncementNotificationChannels.InApp, cancellationToken);

        return hasInApp ? "Sent" : "Skipped";
    }

    private static string ToNotificationPriority(string priority)
    {
        return priority switch
        {
            AnnouncementPriorities.Critical => "Critical",
            AnnouncementPriorities.Important => "High",
            _ => "Information"
        };
    }

    private static string ToDeliveryStatus(string lineStatus)
    {
        return lineStatus switch
        {
            "Sent" => AnnouncementNotificationDeliveryStatuses.Sent,
            "Failed" => AnnouncementNotificationDeliveryStatuses.Failed,
            "Disabled" => AnnouncementNotificationDeliveryStatuses.Skipped,
            _ => AnnouncementNotificationDeliveryStatuses.Queued
        };
    }

    private static string? SanitizeLineError(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= 1000 ? value : value[..1000];
    }

    private static string BuildLineFlexPayload(Announcement announcement, string publicAppUrl)
    {
        var detailUrl = BuildUrl(publicAppUrl, $"/announcements/{announcement.Id}");
        var header = announcement.Priority == AnnouncementPriorities.Critical
            ? "ประกาศเร่งด่วนจาก HOP"
            : announcement.Priority == AnnouncementPriorities.Important
                ? "ประกาศสำคัญจาก HOP"
                : "ประกาศใหม่จาก HOP";
        var priorityColor = announcement.Priority == AnnouncementPriorities.Critical
            ? "#EF4444"
            : announcement.Priority == AnnouncementPriorities.Important
                ? "#F59E0B"
                : "#0F766E";
        var priorityLabel = announcement.Priority == AnnouncementPriorities.Critical
            ? "เร่งด่วน"
            : announcement.Priority == AnnouncementPriorities.Important
                ? "สำคัญ"
                : "ปกติ";
        var title = Truncate(announcement.Title, 90);
        var summary = Truncate(announcement.Summary, 360);
        var category = string.IsNullOrWhiteSpace(announcement.Category?.Name) ? "-" : announcement.Category.Name;
        var publishedAt = announcement.PublishedAt ?? announcement.PublishAt ?? announcement.CreatedAt;
        var altText = Truncate($"{header}: {announcement.Title}", 390);

        return JsonSerializer.Serialize(new
        {
            to = string.Empty,
            messages = new object[]
            {
                new
                {
                    type = "flex",
                    altText,
                    contents = new
                    {
                        type = "bubble",
                        size = "mega",
                        header = new
                        {
                            type = "box",
                            layout = "vertical",
                            backgroundColor = "#064E3B",
                            paddingAll = "20px",
                            spacing = "md",
                            contents = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = header,
                                    color = "#FFFFFF",
                                    weight = "bold",
                                    size = "xl",
                                    wrap = true
                                },
                                new
                                {
                                    type = "text",
                                    text = title,
                                    color = "#D4AF37",
                                    weight = "bold",
                                    size = "lg",
                                    wrap = true
                                },
                                new
                                {
                                    type = "box",
                                    layout = "baseline",
                                    spacing = "sm",
                                    contents = new object[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = "สถานะ",
                                            color = "#D1FAE5",
                                            size = "sm",
                                            flex = 2
                                        },
                                        new
                                        {
                                            type = "text",
                                            text = priorityLabel,
                                            color = "#FFFFFF",
                                            size = "sm",
                                            weight = "bold",
                                            flex = 5
                                        }
                                    }
                                }
                            }
                        },
                        body = new
                        {
                            type = "box",
                            layout = "vertical",
                            paddingAll = "20px",
                            spacing = "md",
                            contents = new object[]
                            {
                                new
                                {
                                    type = "text",
                                    text = summary,
                                    size = "md",
                                    color = "#1F2937",
                                    wrap = true
                                },
                                new
                                {
                                    type = "separator",
                                    margin = "md",
                                    color = "#E4E0D7"
                                },
                                InfoRow("เลขที่ประกาศ", ShortId(announcement.Id), "#C8A96B"),
                                InfoRow("หมวดหมู่", category, "#0F766E"),
                                InfoRow("เผยแพร่", FormatThaiDateTime(publishedAt), "#0F766E"),
                                InfoRow("ความสำคัญ", priorityLabel, priorityColor),
                                new
                                {
                                    type = "box",
                                    layout = "vertical",
                                    backgroundColor = "#F8FAFC",
                                    cornerRadius = "12px",
                                    paddingAll = "12px",
                                    margin = "md",
                                    contents = new object[]
                                    {
                                        new
                                        {
                                            type = "text",
                                            text = "กดปุ่มด้านล่างเพื่ออ่านรายละเอียดประกาศฉบับเต็ม",
                                            size = "sm",
                                            color = "#64748B",
                                            wrap = true
                                        }
                                    }
                                }
                            }
                        },
                        footer = new
                        {
                            type = "box",
                            layout = "vertical",
                            paddingAll = "16px",
                            spacing = "sm",
                            contents = new object[]
                            {
                                new
                                {
                                    type = "button",
                                    style = "primary",
                                    height = "sm",
                                    color = "#0F766E",
                                    action = new
                                    {
                                        type = "uri",
                                        label = "ดูรายละเอียด",
                                        uri = detailUrl
                                    }
                                }
                            }
                        },
                        styles = new
                        {
                            footer = new
                            {
                                separator = true
                            }
                        }
                    }
                }
            }
        });
    }

    private static object InfoRow(string label, string value, string valueColor)
    {
        return new
        {
            type = "box",
            layout = "baseline",
            spacing = "sm",
            contents = new object[]
            {
                new
                {
                    type = "text",
                    text = label,
                    color = "#64748B",
                    size = "sm",
                    flex = 3
                },
                new
                {
                    type = "text",
                    text = ":",
                    color = "#C8A96B",
                    size = "sm",
                    flex = 0
                },
                new
                {
                    type = "text",
                    text = string.IsNullOrWhiteSpace(value) ? "-" : value,
                    color = valueColor,
                    size = "sm",
                    weight = "bold",
                    wrap = true,
                    flex = 6
                }
            }
        };
    }

    private static string BuildUrl(string publicAppUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(publicAppUrl))
        {
            return path;
        }

        return $"{publicAppUrl.TrimEnd('/')}/{path.TrimStart('/')}";
    }

    private static string ShortId(Guid id)
    {
        return id.ToString("N")[..8].ToUpperInvariant();
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : $"{trimmed[..maxLength]}...";
    }

    private static string FormatThaiDateTime(DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
        var thaiTime = utc.AddHours(7);
        return $"{thaiTime:dd/MM}/{thaiTime.Year + 543} {thaiTime:HH:mm}";
    }
}
