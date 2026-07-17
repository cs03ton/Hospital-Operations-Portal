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
        logger.LogInformation("LINE webhook received. BodyLength={BodyLength} HasSignature={HasSignature}", body.Length, !string.IsNullOrWhiteSpace(signature));

        if (!VerifySignature(body, signature))
        {
            logger.LogWarning("LINE webhook signature invalid. HasSecret={HasSecret}", lineConfiguration.HasChannelSecret);
            return Unauthorized(ApiResponse<string>.Fail("Invalid LINE signature."));
        }

        logger.LogInformation("LINE webhook signature valid.");

        var results = new List<LineWebhookHandleResult>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "LINE webhook malformed JSON.");
            return BadRequest(ApiResponse<string>.Fail("Malformed LINE webhook JSON."));
        }

        using (document)
        {
            var destination = document.RootElement.TryGetProperty("destination", out var destinationProperty)
                ? destinationProperty.GetString()
                : null;

            if (!document.RootElement.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            {
                logger.LogInformation("LINE webhook accepted without events array. Destination={Destination}", MaskLineId(destination));
                return ApiResponse<IReadOnlyList<LineWebhookHandleResult>>.Ok(results);
            }

            var eventCount = events.GetArrayLength();
            logger.LogInformation("LINE webhook parsed. Destination={Destination} EventCount={EventCount}", MaskLineId(destination), eventCount);

            if (eventCount == 0)
            {
                logger.LogInformation("LINE webhook verification request detected. Destination={Destination} EventCount=0", MaskLineId(destination));
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
                    logger.LogInformation("LINE webhook event ignored because type or user id is missing. EventType={EventType}", eventType);
                    continue;
                }

                try
                {
                    var result = eventType switch
                    {
                        "follow" => await lineUserBindingService.HandleFollowAsync(lineUserId, cancellationToken),
                        "unfollow" => await lineUserBindingService.HandleUnfollowAsync(lineUserId, cancellationToken),
                        "message" => await HandleMessageEvent(lineEvent, lineUserId, cancellationToken),
                        _ => new LineWebhookHandleResult(eventType, null, "Ignored", false, "Unsupported LINE webhook event.")
                    };
                    results.Add(result);
                    logger.LogInformation(
                        "LINE webhook event processed. EventType={EventType} LineUserId={LineUserId} Status={Status} Bound={Bound}",
                        eventType,
                        MaskLineId(lineUserId),
                        result.Status,
                        result.Bound);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "LINE webhook event processing failed. EventType={EventType} LineUserId={LineUserId}", eventType, MaskLineId(lineUserId));
                    results.Add(new LineWebhookHandleResult(eventType, MaskLineId(lineUserId), "Failed", false, "Webhook event processing failed."));
                }
            }
        }

        logger.LogInformation("LINE webhook processing completed. ProcessedCount={ProcessedCount}", results.Count);

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

    private static string? MaskLineId(string? lineUserId)
    {
        if (string.IsNullOrWhiteSpace(lineUserId))
        {
            return null;
        }

        var trimmed = lineUserId.Trim();
        if (trimmed.Length <= 10)
        {
            return "***";
        }

        return $"{trimmed[..5]}...{trimmed[^4..]}";
    }
}
