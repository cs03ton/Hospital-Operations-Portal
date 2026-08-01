using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed class LineLiffAuthenticationService(
    LineConfigurationResolver lineConfiguration,
    HttpClient httpClient,
    ILogger<LineLiffAuthenticationService> logger) : ILineLiffAuthenticationService
{
    private const int MaxIdTokenLength = 8192;

    public async Task<VerifiedLineIdentity> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken) || idToken.Length > MaxIdTokenLength)
        {
            logger.LogWarning("LINE LIFF ID token rejected. Reason={Reason}", "MissingOrTooLong");
            throw new UnauthorizedAccessException("LINE_TOKEN_INVALID");
        }

        if (string.IsNullOrWhiteSpace(lineConfiguration.LoginChannelId))
        {
            logger.LogError("LINE LIFF login channel id is not configured.");
            throw new InvalidOperationException("LINE_LOGIN_CHANNEL_ID_MISSING");
        }

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id_token"] = idToken,
            ["client_id"] = lineConfiguration.LoginChannelId
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, lineConfiguration.IdTokenVerifyUrl)
        {
            Content = content
        };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("LINE LIFF ID token verification timed out.");
            throw new InvalidOperationException("LINE_SERVICE_UNAVAILABLE");
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "LINE LIFF ID token verification request failed.");
            throw new InvalidOperationException("LINE_SERVICE_UNAVAILABLE");
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("LINE LIFF ID token verification rejected. StatusCode={StatusCode}", (int)response.StatusCode);
            throw new UnauthorizedAccessException("LINE_TOKEN_INVALID");
        }

        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var root = document.RootElement;
        var audience = GetString(root, "aud");
        var subject = GetString(root, "sub");
        var expiresAtUnix = GetInt64(root, "exp");

        if (string.IsNullOrWhiteSpace(subject) ||
            string.IsNullOrWhiteSpace(audience) ||
            !string.Equals(audience, lineConfiguration.LoginChannelId, StringComparison.Ordinal) ||
            expiresAtUnix is null)
        {
            logger.LogWarning("LINE LIFF ID token rejected. Reason={Reason}, AudienceMatched={AudienceMatched}",
                "InvalidClaims",
                string.Equals(audience, lineConfiguration.LoginChannelId, StringComparison.Ordinal));
            throw new UnauthorizedAccessException("LINE_TOKEN_INVALID");
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix.Value).UtcDateTime;
        if (expiresAt <= DateTime.UtcNow)
        {
            logger.LogWarning("LINE LIFF ID token rejected. Reason={Reason}", "Expired");
            throw new UnauthorizedAccessException("LINE_TOKEN_EXPIRED");
        }

        logger.LogInformation("LINE LIFF ID token verified. LineUser={LineUser}, Exp={ExpiresAt}", MaskLineUserId(subject), expiresAt);
        return new VerifiedLineIdentity(
            subject,
            audience,
            expiresAt,
            GetString(root, "name"),
            GetString(root, "picture"),
            GetString(root, "email"));
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static long? GetInt64(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var number) => number,
            JsonValueKind.String when long.TryParse(value.GetString(), out var number) => number,
            _ => null
        };
    }

    private static string MaskLineUserId(string value)
    {
        return value.Length <= 10 ? "U********" : $"{value[..5]}...{value[^4..]}";
    }
}
