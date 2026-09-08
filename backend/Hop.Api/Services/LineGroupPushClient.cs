using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Models;
using Microsoft.AspNetCore.DataProtection;

namespace Hop.Api.Services;

public sealed record LineGroupPushResult(bool Success, bool IsTransient, bool DestinationUnavailable, string? ErrorCode, string? ErrorMessage);

public interface ILineGroupPushClient
{
    Task<LineGroupPushResult> PushTextAsync(string groupId, string text, CancellationToken ct);
    Task<LineGroupPushResult> PushTextAsync(LineGroupDestination destination, string text, CancellationToken ct) => PushTextAsync(destination.LineGroupId, text, ct);
    Task<LineGroupPushResult> PushMessageAsync(LineGroupDestination destination, FleetGroupRenderedMessage message, CancellationToken ct) =>
        PushTextAsync(destination, message.Text, ct);
}

public sealed class LineGroupPushClient(
    LineConfigurationResolver lineConfiguration,
    HttpClient httpClient,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<LineGroupPushClient> logger) : ILineGroupPushClient
{
    private readonly IDataProtector credentialProtector = dataProtectionProvider.CreateProtector("HOP.LineGroupDestinationCredentials.v1");

    public Task<LineGroupPushResult> PushMessageAsync(LineGroupDestination destination, FleetGroupRenderedMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.FlexContentsJson))
            return PushTextAsync(destination, message.Text, ct);
        using var contents = JsonDocument.Parse(message.FlexContentsJson);
        var lineMessage = new
        {
            type = "flex",
            altText = string.IsNullOrWhiteSpace(message.AltText) ? "แจ้งเตือนคำขอใช้รถ HOP" : message.AltText,
            contents = contents.RootElement.Clone()
        };
        return PushPayloadAsync(destination, lineMessage, ct);
    }

    public async Task<LineGroupPushResult> PushTextAsync(LineGroupDestination destination, string text, CancellationToken ct)
    {
        if (!string.Equals(destination.DeliveryProvider, "CUSTOM_ENDPOINT", StringComparison.OrdinalIgnoreCase))
            return await PushTextAsync(destination.LineGroupId, text, ct);
        return await PushPayloadAsync(destination, new { type = "text", text }, ct);
    }

    private async Task<LineGroupPushResult> PushPayloadAsync(LineGroupDestination destination, object lineMessage, CancellationToken ct)
    {
        if (!string.Equals(destination.DeliveryProvider, "CUSTOM_ENDPOINT", StringComparison.OrdinalIgnoreCase))
            return await PushLineApiPayloadAsync(destination.LineGroupId, lineMessage, ct);
        if (string.IsNullOrWhiteSpace(destination.EndpointUrl) || string.IsNullOrWhiteSpace(destination.ClientId) || string.IsNullOrWhiteSpace(destination.ClientSecretProtected))
            return new(false, false, false, "CUSTOM_ENDPOINT_NOT_CONFIGURED", "Custom LINE endpoint credentials are incomplete.");
        try
        {
            var secret = credentialProtector.Unprotect(destination.ClientSecretProtected);
            using var request = new HttpRequestMessage(HttpMethod.Post, destination.EndpointUrl);
            request.Headers.TryAddWithoutValidation("client-key", destination.ClientId);
            request.Headers.TryAddWithoutValidation("secret-key", secret);
            request.Content = new StringContent(
                JsonSerializer.Serialize(new { messages = new[] { lineMessage } }),
                Encoding.UTF8,
                "application/json");
            using var response = await httpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            if (response.IsSuccessStatusCode)
            {
                var applicationFailure = ParseApplicationFailure(responseBody);
                if (applicationFailure is null) return new(true, false, false, null, null);
                var transientApplicationFailure = applicationFailure.Value.StatusCode is 408 or 429 || applicationFailure.Value.StatusCode >= 500;
                var authenticationFailure = applicationFailure.Value.StatusCode is 401 or 403;
                return new(
                    false,
                    transientApplicationFailure,
                    false,
                    $"CUSTOM_RESPONSE_{applicationFailure.Value.StatusCode}",
                    authenticationFailure ? "Notification endpoint authentication failed." : "Notification endpoint reported that delivery failed.");
            }
            var transient = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            var unavailable = response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Gone;
            logger.LogWarning("Custom LINE endpoint failed. Destination={DestinationId} StatusCode={StatusCode}", destination.Id, (int)response.StatusCode);
            return new(false, transient, unavailable, $"CUSTOM_HTTP_{(int)response.StatusCode}", transient ? "Notification endpoint is temporarily unavailable." : "Notification endpoint rejected the message.");
        }
        catch (CryptographicException ex)
        {
            logger.LogError(ex, "Unable to decrypt LINE group credentials. Destination={DestinationId}", destination.Id);
            return new(false, false, false, "CUSTOM_CREDENTIAL_DECRYPTION_FAILED", "Stored endpoint credentials cannot be decrypted.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or UriFormatException)
        {
            logger.LogWarning(ex, "Custom LINE endpoint request failed. Destination={DestinationId}", destination.Id);
            return new(false, true, false, "CUSTOM_ENDPOINT_NETWORK_ERROR", "Unable to reach notification endpoint.");
        }
    }

    private async Task<LineGroupPushResult> PushLineApiPayloadAsync(string groupId, object lineMessage, CancellationToken ct)
    {
        if (!lineConfiguration.Enabled || string.IsNullOrWhiteSpace(lineConfiguration.AccessToken))
            return new(false, false, false, "LINE_NOT_CONFIGURED", "LINE Messaging is disabled or access token is missing.");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, lineConfiguration.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", lineConfiguration.AccessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(new { to = groupId, messages = new[] { lineMessage } }), Encoding.UTF8, "application/json");
            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return new(true, false, false, null, null);
            var transient = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            var unavailable = response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Gone;
            return new(false, transient, unavailable, $"LINE_HTTP_{(int)response.StatusCode}", transient ? "LINE API is temporarily unavailable." : "LINE destination rejected the message.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (HttpRequestException) { return new(false, true, false, "LINE_NETWORK_ERROR", "Unable to reach LINE API."); }
        catch (TaskCanceledException) { return new(false, true, false, "LINE_TIMEOUT", "LINE API request timed out."); }
    }

    private static (int StatusCode, string? Message)? ParseApplicationFailure(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody)) return null;
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;
            var statusCode = 0;
            if (root.TryGetProperty("status", out var status))
            {
                if (status.ValueKind == JsonValueKind.Number) status.TryGetInt32(out statusCode);
                else if (status.ValueKind == JsonValueKind.String) int.TryParse(status.GetString(), out statusCode);
            }
            var explicitlyFailed = root.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.False;
            if (statusCode < 400 && !explicitlyFailed) return null;
            var message = root.TryGetProperty("message", out var messageNode) && messageNode.ValueKind == JsonValueKind.String ? messageNode.GetString() : null;
            return (statusCode >= 400 ? statusCode : 400, message);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<LineGroupPushResult> PushTextAsync(string groupId, string text, CancellationToken ct)
    {
        if (!lineConfiguration.Enabled || string.IsNullOrWhiteSpace(lineConfiguration.AccessToken))
            return new(false, false, false, "LINE_NOT_CONFIGURED", "LINE Messaging is disabled or access token is missing.");
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, lineConfiguration.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", lineConfiguration.AccessToken);
            request.Content = new StringContent(JsonSerializer.Serialize(new
            {
                to = groupId,
                messages = new[] { new { type = "text", text } }
            }), Encoding.UTF8, "application/json");
            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode) return new(true, false, false, null, null);

            var code = $"LINE_HTTP_{(int)response.StatusCode}";
            var transient = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            var unavailable = response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Gone;
            logger.LogWarning("LINE group push failed. GroupId={GroupId} StatusCode={StatusCode}", LineGroupRegistrationService.Mask(groupId), (int)response.StatusCode);
            return new(false, transient, unavailable, code, transient ? "LINE API is temporarily unavailable." : "LINE destination rejected the message.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "LINE group push network failure. GroupId={GroupId}", LineGroupRegistrationService.Mask(groupId));
            return new(false, true, false, "LINE_NETWORK_ERROR", "Unable to reach LINE API.");
        }
        catch (TaskCanceledException ex)
        {
            logger.LogWarning(ex, "LINE group push timeout. GroupId={GroupId}", LineGroupRegistrationService.Mask(groupId));
            return new(false, true, false, "LINE_TIMEOUT", "LINE API request timed out.");
        }
    }
}
