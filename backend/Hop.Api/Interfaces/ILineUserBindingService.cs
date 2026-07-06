using Hop.Api.DTOs;

namespace Hop.Api.Interfaces;

public interface ILineUserBindingService
{
    Task<LineBindingStatusResponse> GetMyBindingStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<LineMeStatusResponse> GetMyLineStatusAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<LinePairingCodeResponse> CreatePairingCodeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<LineConnectTokenResponse> CreateConnectTokenAsync(Guid userId, string? ipAddress = null, CancellationToken cancellationToken = default);
    Task<LineBindingStatusResponse> UnbindAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<LineWebhookHandleResult> HandleFollowAsync(string lineUserId, CancellationToken cancellationToken = default);
    Task<LineWebhookHandleResult> HandleUnfollowAsync(string lineUserId, CancellationToken cancellationToken = default);
    Task<LineWebhookHandleResult> HandleMessageAsync(string lineUserId, string messageText, CancellationToken cancellationToken = default);
}
