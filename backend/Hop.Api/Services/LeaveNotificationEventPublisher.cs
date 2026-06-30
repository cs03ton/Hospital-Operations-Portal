using Hop.Api.Data;
using Hop.Api.Configuration;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Hop.Api.Services;

public class LeaveNotificationEventPublisher(
    AppDbContext db,
    ILineMessagingService lineMessagingService,
    LineConfigurationResolver lineConfiguration,
    IUserAvatarUrlResolver avatarUrlResolver,
    ILogger<LeaveNotificationEventPublisher> logger) : ILeaveNotificationEventPublisher
{
    public async Task PublishAsync(string eventName, Guid leaveRequestId, Guid? recipientUserId, CancellationToken cancellationToken = default)
    {
        var leaveRequest = await db.LeaveRequests
            .Include(item => item.User)
                .ThenInclude(item => item!.Department)
            .Include(item => item.LeaveType)
            .Include(item => item.Approvals)
                .ThenInclude(item => item.Approver)
            .FirstOrDefaultAsync(item => item.Id == leaveRequestId, cancellationToken);
        if (leaveRequest is null)
        {
            logger.LogWarning("Leave notification skipped: leave request {LeaveRequestId} not found.", leaveRequestId);
            return;
        }

        logger.LogInformation(
            "Leave notification event prepared. Event={EventName}, LeaveRequestId={LeaveRequestId}, RecipientUserId={RecipientUserId}",
            eventName,
            leaveRequestId,
            recipientUserId);

        var avatar = avatarUrlResolver.ResolveForLine(leaveRequest.User);

        switch (eventName)
        {
            case "LeaveSubmitted":
                if (recipientUserId is not null)
                {
                    await CreateApproverNotificationAsync(
                        leaveRequest,
                        recipientUserId.Value,
                        "LeaveApprovalFlexCardSent",
                        $"คำขอลา {RequestCode(leaveRequest)} รออนุมัติ",
                        $"{leaveRequest.User?.FullName ?? "-"} · {leaveRequest.LeaveType?.Name ?? "-"} · {FormatDateRange(leaveRequest)}",
                        LeaveLineFlexMessageTemplates.BuildPendingApprovalCard(leaveRequest, lineConfiguration.PublicAppUrl, avatar),
                        cancellationToken);
                }
                break;

            case "ApprovalStepActivated":
                if (recipientUserId is not null)
                {
                    var previousApprover = leaveRequest.Approvals
                        .Where(item => item.Status == "Approved")
                        .OrderByDescending(item => item.ActionAt)
                        .Select(item => item.Approver?.FullName)
                        .FirstOrDefault();
                    await CreateApproverNotificationAsync(
                        leaveRequest,
                        recipientUserId.Value,
                        "LeaveApprovalFlexCardSent",
                        $"คำขอลา {RequestCode(leaveRequest)} รออนุมัติขั้นถัดไป",
                        $"อนุมัติแล้วโดย {previousApprover ?? "-"} · {leaveRequest.User?.FullName ?? "-"} · {leaveRequest.LeaveType?.Name ?? "-"}",
                        LeaveLineFlexMessageTemplates.BuildNextApproverCard(leaveRequest, lineConfiguration.PublicAppUrl, avatar),
                        cancellationToken);
                }
                break;

            case "LeaveApproved":
                await ClearActionRequiredAsync(leaveRequest.Id, null, cancellationToken);
                await CreateRequesterNotificationAsync(
                    leaveRequest,
                    "LeaveApprovedToRequester",
                    $"คำขอลา {RequestCode(leaveRequest)} อนุมัติแล้ว",
                    $"คำขอลา {leaveRequest.LeaveType?.Name ?? "-"} วันที่ {FormatDateRange(leaveRequest)} ได้รับการอนุมัติแล้ว",
                    "Success",
                    LeaveLineFlexMessageTemplates.BuildApprovedToRequesterCard(leaveRequest, lineConfiguration.PublicAppUrl, avatar),
                    cancellationToken);
                break;

            case "LeaveRejected":
                await ClearActionRequiredAsync(leaveRequest.Id, null, cancellationToken);
                var rejectedApproval = leaveRequest.Approvals
                    .Where(item => item.Status == "Rejected")
                    .OrderByDescending(item => item.ActionAt)
                    .FirstOrDefault();
                await CreateRequesterNotificationAsync(
                    leaveRequest,
                    "LeaveRejectedToRequester",
                    $"คำขอลา {RequestCode(leaveRequest)} ไม่อนุมัติ",
                    $"ผู้พิจารณา: {rejectedApproval?.Approver?.FullName ?? "-"} · เหตุผล: {Blank(rejectedApproval?.Remark)}",
                    "High",
                    LeaveLineFlexMessageTemplates.BuildRejectedToRequesterCard(leaveRequest, lineConfiguration.PublicAppUrl, avatar),
                    cancellationToken);
                break;

            case "LeaveCancelled":
                await ClearActionRequiredAsync(leaveRequest.Id, null, cancellationToken);
                if (recipientUserId is not null)
                {
                    await CreateInformationNotificationAsync(
                        leaveRequest,
                        recipientUserId.Value,
                        "LeaveCancelledToApprover",
                        $"คำขอลา {RequestCode(leaveRequest)} ถูกยกเลิกแล้ว",
                        $"{leaveRequest.User?.FullName ?? "-"} ยกเลิกคำขอลาแล้ว",
                        "Information",
                        LeaveLineFlexMessageTemplates.BuildCancelledCard(leaveRequest, lineConfiguration.PublicAppUrl, avatar),
                        cancellationToken);
                }

                await CreateRequesterNotificationAsync(
                    leaveRequest,
                    "LeaveCancelledToRequester",
                    $"ยกเลิกคำขอลา {RequestCode(leaveRequest)} แล้ว",
                    "ยกเลิกคำขอลาเรียบร้อยแล้ว",
                    "Information",
                    null,
                    cancellationToken);
                break;
        }
    }

    private async Task CreateApproverNotificationAsync(
        LeaveRequest leaveRequest,
        Guid recipientUserId,
        string eventType,
        string title,
        string message,
        string lineMessage,
        CancellationToken cancellationToken)
    {
        await ClearActionRequiredAsync(leaveRequest.Id, recipientUserId, cancellationToken);
        await CreateNotificationAsync(leaveRequest, recipientUserId, eventType, title, message, "ActionRequired", "High", lineMessage, cancellationToken);
    }

    private async Task CreateRequesterNotificationAsync(
        LeaveRequest leaveRequest,
        string eventType,
        string title,
        string message,
        string priority,
        string? lineMessage,
        CancellationToken cancellationToken)
    {
        await CreateNotificationAsync(leaveRequest, leaveRequest.UserId, eventType, title, message, "Information", priority, lineMessage, cancellationToken);
    }

    private async Task CreateInformationNotificationAsync(
        LeaveRequest leaveRequest,
        Guid recipientUserId,
        string eventType,
        string title,
        string message,
        string priority,
        string? lineMessage,
        CancellationToken cancellationToken)
    {
        await CreateNotificationAsync(leaveRequest, recipientUserId, eventType, title, message, "Information", priority, lineMessage, cancellationToken);
    }

    private async Task CreateNotificationAsync(
        LeaveRequest leaveRequest,
        Guid recipientUserId,
        string eventType,
        string title,
        string message,
        string notificationType,
        string priority,
        string? lineMessage,
        CancellationToken cancellationToken)
    {
        var referenceId = leaveRequest.Id.ToString();
        var exists = await db.Notifications.AnyAsync(item =>
            item.UserId == recipientUserId &&
            item.ReferenceEntity == eventType &&
            item.ReferenceId == referenceId &&
            item.ArchivedAt == null,
            cancellationToken);
        if (exists)
        {
            logger.LogInformation("Leave notification skipped duplicate. Event={EventType}, LeaveRequestId={LeaveRequestId}, RecipientUserId={RecipientUserId}", eventType, leaveRequest.Id, recipientUserId);
            return;
        }

        db.Notifications.Add(new Notification
        {
            UserId = recipientUserId,
            Channel = "InApp",
            Category = "Leave",
            NotificationType = notificationType,
            Priority = priority,
            Title = title,
            Message = message,
            ActionUrl = $"/leave/{leaveRequest.Id}",
            ReferenceEntity = eventType,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(lineMessage))
        {
            try
            {
                if (lineMessage.TrimStart().StartsWith('{'))
                {
                    await lineMessagingService.NotifyUserPayloadAsync(recipientUserId, eventType, lineMessage, leaveRequest.Id, cancellationToken);
                }
                else
                {
                    await lineMessagingService.NotifyUserAsync(recipientUserId, eventType, lineMessage, leaveRequest.Id, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "LINE notification failed but leave workflow continues. Event={EventType}, LeaveRequestId={LeaveRequestId}, RecipientUserId={RecipientUserId}", eventType, leaveRequest.Id, recipientUserId);
            }
        }
    }

    private async Task ClearActionRequiredAsync(Guid leaveRequestId, Guid? userId, CancellationToken cancellationToken)
    {
        var referenceId = leaveRequestId.ToString();
        var query = db.Notifications.Where(item =>
            item.ReferenceId == referenceId &&
            item.NotificationType == "ActionRequired" &&
            item.ArchivedAt == null);

        if (userId is not null)
        {
            query = query.Where(item => item.UserId == userId.Value);
        }

        var items = await query.ToListAsync(cancellationToken);
        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAt ??= DateTime.UtcNow;
            item.ArchivedAt = DateTime.UtcNow;
        }

        if (items.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static string RequestCode(LeaveRequest request)
    {
        return request.RequestNumber ?? request.Id.ToString("N")[..8].ToUpperInvariant();
    }

    private static string FormatDateRange(LeaveRequest request)
    {
        return $"{FormatDate(request.StartDate)} - {FormatDate(request.EndDate)}";
    }

    private static string FormatDate(DateOnly date)
    {
        return $"{date.Day:00}/{date.Month:00}/{date.Year + 543}";
    }

    private static string Blank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}
