using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/line/webhook")]
[AllowAnonymous]
public class LineWebhookController(
    LineConfigurationResolver lineConfiguration,
    ILineUserBindingService lineUserBindingService,
    ILogger<LineWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LineWebhookHandleResult>>>> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["X-Line-Signature"].FirstOrDefault();

        if (!VerifySignature(body, signature))
        {
            logger.LogWarning("LINE webhook signature verification failed. HasSecret={HasSecret}", lineConfiguration.HasChannelSecret);
            return Unauthorized(ApiResponse<string>.Fail("Invalid LINE signature."));
        }

        var results = new List<LineWebhookHandleResult>();
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
        {
            return ApiResponse<IReadOnlyList<LineWebhookHandleResult>>.Ok(results);
        }

        foreach (var lineEvent in events.EnumerateArray())
        {
            var eventType = lineEvent.TryGetProperty("type", out var typeProperty) ? typeProperty.GetString() : null;
            var lineUserId = lineEvent.TryGetProperty("source", out var source) &&
                source.TryGetProperty("userId", out var userIdProperty)
                    ? userIdProperty.GetString()
                    : null;

            if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(lineUserId))
            {
                continue;
            }

            var result = eventType switch
            {
                "follow" => await lineUserBindingService.HandleFollowAsync(lineUserId, cancellationToken),
                "unfollow" => await lineUserBindingService.HandleUnfollowAsync(lineUserId, cancellationToken),
                "message" => await HandleMessageEvent(lineEvent, lineUserId, cancellationToken),
                _ => new LineWebhookHandleResult(eventType, null, "Ignored", false, "Unsupported LINE webhook event.")
            };
            results.Add(result);
        }

        return ApiResponse<IReadOnlyList<LineWebhookHandleResult>>.Ok(results);
    }

    private async Task<LineWebhookHandleResult> HandleMessageEvent(JsonElement lineEvent, string lineUserId, CancellationToken cancellationToken)
    {
        if (!lineEvent.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("type", out var messageType) ||
            !string.Equals(messageType.GetString(), "text", StringComparison.Ordinal) ||
            !message.TryGetProperty("text", out var textProperty))
        {
            return new LineWebhookHandleResult("message", null, "Ignored", false, "Only text messages are supported for LINE binding.");
        }

        return await lineUserBindingService.HandleMessageAsync(lineUserId, textProperty.GetString() ?? string.Empty, cancellationToken);
    }

    private bool VerifySignature(string body, string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature) || string.IsNullOrWhiteSpace(lineConfiguration.ChannelSecret))
        {
            return false;
        }

        var key = Encoding.UTF8.GetBytes(lineConfiguration.ChannelSecret);
        var payload = Encoding.UTF8.GetBytes(body);
        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(payload);
        var expected = Convert.ToBase64String(hash);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }
}
