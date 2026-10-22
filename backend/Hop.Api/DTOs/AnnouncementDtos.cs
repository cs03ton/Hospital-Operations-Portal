namespace Hop.Api.DTOs;

public record AnnouncementCategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    string? Color,
    bool IsActive,
    int DisplayOrder
);

public record AnnouncementTargetRequest(string TargetType, string? TargetValue);

public record AnnouncementTargetResponse(
    Guid Id,
    string TargetType,
    string? TargetValue
);

public record AnnouncementFileResponse(
    Guid Id,
    string FileName,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    string FileRole,
    string DownloadUrl
);

public record AnnouncementImageResponse(
    Guid Id,
    string ThumbnailUrl,
    string MediumUrl,
    string LargeUrl,
    string OriginalUrl,
    int DisplayOrder,
    bool IsCover,
    int? Width,
    int? Height,
    long FileSize
);

public record AnnouncementCapabilitiesResponse(
    bool CanEdit,
    bool CanPublish,
    bool CanDuplicate,
    bool CanArchive,
    bool CanCancel,
    bool CanDelete
);

public record AnnouncementSummaryResponse(
    Guid Id,
    string Title,
    string Summary,
    string Status,
    string Priority,
    AnnouncementCategoryResponse? Category,
    bool IsFeatured,
    bool ShowAsPopup,
    bool ShowAsBanner,
    bool RequiresAcknowledgement,
    bool IsRead,
    bool IsAcknowledged,
    DateTime? PublishAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    string? CreatedByName,
    AnnouncementImageResponse? CoverImage,
    string? LegacyCoverImageUrl,
    string? Tags,
    int ViewCount,
    int AcknowledgedCount,
    bool NotifyInApp,
    bool NotifyViaLine,
    string? NotificationDispatchStatus,
    int InAppRecipientCount,
    int LineEligibleRecipientCount,
    int LineQueuedCount,
    int LineSentCount,
    int LineFailedCount,
    AnnouncementCapabilitiesResponse? Capabilities = null,
    bool NotifyViaLineGroup = false
);

public record AnnouncementDetailResponse(
    Guid Id,
    string Title,
    string Summary,
    string Body,
    string Status,
    string Priority,
    AnnouncementCategoryResponse? Category,
    IReadOnlyList<AnnouncementTargetResponse> Targets,
    IReadOnlyList<AnnouncementFileResponse> Files,
    AnnouncementImageResponse? CoverImage,
    IReadOnlyList<AnnouncementImageResponse> Images,
    bool IsFeatured,
    bool ShowAsPopup,
    bool ShowAsBanner,
    bool RequiresAcknowledgement,
    bool IsRead,
    bool IsAcknowledged,
    DateTime? ReadAt,
    DateTime? AcknowledgedAt,
    DateTime? PublishAt,
    DateTime? ExpiresAt,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    DateTime? UpdatedAt,
    string? CreatedByName,
    string? PublishedByName,
    string? LegacyCoverImageUrl,
    string? Tags,
    int ViewCount,
    int AcknowledgedCount,
    bool NotifyInApp,
    bool NotifyViaLine,
    string? NotificationDispatchStatus,
    string? NotificationDispatchError,
    DateTime? NotificationSentAt,
    DateTime? LineNotificationQueuedAt,
    int InAppRecipientCount,
    int LineEligibleRecipientCount,
    int LineQueuedCount,
    int LineSentCount,
    int LineFailedCount,
    AnnouncementCapabilitiesResponse? Capabilities = null,
    bool NotifyViaLineGroup = false,
    IReadOnlyList<Guid>? LineGroupIds = null,
    IReadOnlyList<AnnouncementLineGroupOptionResponse>? LineGroups = null
);

public record CreateAnnouncementRequest(
    string Title,
    string? Summary,
    string Body,
    string Priority,
    Guid? CategoryId,
    DateTime? PublishAt,
    DateTime? ExpiresAt,
    bool IsFeatured,
    bool ShowAsPopup,
    bool ShowAsBanner,
    bool RequiresAcknowledgement,
    string? Tags,
    bool? NotifyInApp,
    bool? NotifyViaLine,
    IReadOnlyList<AnnouncementTargetRequest>? Targets,
    bool NotifyViaLineGroup = false,
    IReadOnlyList<Guid>? LineGroupIds = null
);

public record UpdateAnnouncementRequest(
    string Title,
    string? Summary,
    string Body,
    string Priority,
    Guid? CategoryId,
    DateTime? PublishAt,
    DateTime? ExpiresAt,
    bool IsFeatured,
    bool ShowAsPopup,
    bool ShowAsBanner,
    bool RequiresAcknowledgement,
    string? Tags,
    bool? NotifyInApp,
    bool? NotifyViaLine,
    IReadOnlyList<AnnouncementTargetRequest>? Targets,
    bool NotifyViaLineGroup = false,
    IReadOnlyList<Guid>? LineGroupIds = null
);

public record AnnouncementAcknowledgeResponse(
    Guid AnnouncementId,
    bool IsRead,
    bool IsAcknowledged,
    DateTime ReadAt,
    DateTime? AcknowledgedAt
);

public record AnnouncementImageOrderRequest(IReadOnlyList<AnnouncementImageOrderItem> Items);

public record AnnouncementImageOrderItem(Guid ImageId, int DisplayOrder);

public record AnnouncementNotificationPreviewRequest(
    bool? NotifyInApp,
    bool? NotifyViaLine
);

public record AnnouncementNotificationPreviewResponse(
    Guid AnnouncementId,
    bool NotifyInApp,
    bool NotifyViaLine,
    int TotalTargetUsers,
    int InAppRecipientCount,
    int LineBoundRecipientCount,
    int LineUnboundRecipientCount,
    int InactiveUserCount,
    int EstimatedQueueItems,
    IReadOnlyList<string> Warnings,
    bool NotifyViaLineGroup = false,
    IReadOnlyList<AnnouncementLineGroupOptionResponse>? LineGroups = null,
    int LineGroupQueueCount = 0
);

public record AnnouncementLineGroupOptionResponse(Guid Id, string DisplayName, string Status);
public record AnnouncementLineGroupDeliveryResponse(Guid Id, Guid DestinationId, string DisplayName,
    string Status, int AttemptCount, string? ErrorCode, string? ErrorMessage,
    DateTime CreatedAt, DateTime? SentAt, DateTime? FailedAt);
