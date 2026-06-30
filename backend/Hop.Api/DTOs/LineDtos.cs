namespace Hop.Api.DTOs;

public record LeaveNotificationMessage(
    Guid LeaveRequestId,
    Guid UserId,
    string Fullname,
    string LeaveTypeName,
    string Status,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Remark
);

public record LineTestSendRequest(
    string? ToUserId,
    string Message
);

public record LineTestSendResponse(
    bool Success,
    string Message,
    string? DeliveryStatus,
    Guid? DeliveryLogId,
    int? HttpStatusCode = null,
    long? ResponseTimeMs = null,
    string? Error = null
);

public record LineSettingsResponse(
    bool Enabled,
    string? ChannelId,
    bool HasChannelSecret,
    bool HasAccessToken,
    string? TestUserId,
    string Endpoint,
    bool ChannelSecretConfigured,
    bool ChannelAccessTokenConfigured
);

public record LineOperationsStatusResponse(
    bool Enabled,
    string ConnectionStatus,
    string? ChannelIdMasked,
    bool HasChannelSecret,
    bool HasAccessToken,
    bool HasTestUserId,
    string? TestUserIdMasked,
    string Endpoint,
    string Environment,
    bool WebhookActive,
    string? BotName,
    DateTime? LastSuccessfulDelivery,
    DateTime? LastFailedDelivery,
    int QueueLength,
    int PendingRetry,
    double? AverageResponseTimeMs,
    IReadOnlyList<LineChecklistItemResponse> Checklist
);

public record LineChecklistItemResponse(
    string Label,
    bool Passed,
    string Recommendation
);

public record LineConnectionValidationResponse(
    bool Success,
    string Message,
    int? HttpStatusCode,
    long ResponseTimeMs,
    string? BotName,
    IReadOnlyList<LineChecklistItemResponse> Checklist
);

public record LineDeliveryLogResponse(
    Guid Id,
    DateTime Date,
    string Recipient,
    string Module,
    string Event,
    string Status,
    int Retry,
    long? DurationMs,
    string? Error
);

public record LineDeliveryLogDetailResponse(
    Guid Id,
    DateTime Date,
    string Recipient,
    string Event,
    string Status,
    int Retry,
    string? ResponseDetail,
    DateTime? NextRetryAt,
    DateTime? SentAt
);

public record LineNotificationSimulatorRequest(
    Guid UserId,
    string EventType,
    string? Message
);

public record LineFlexPreviewResponse(
    string Payload,
    IReadOnlyList<LineChecklistItemResponse> Validation
);

public record LineFlexValidateRequest(
    string Payload
);

public record LineFlexValidateResponse(
    bool IsValid,
    string Message,
    IReadOnlyList<LineChecklistItemResponse> Checks
);

public record LineFlexTestSendRequest(
    string? ToUserId,
    Guid? LeaveRequestId,
    string? Payload,
    string? Variant = null,
    string? AvatarMode = null
);
