using System.Text.RegularExpressions;
using Hop.Api.Interfaces;

namespace Hop.Api.Services;

public sealed partial class DiagnosticsRedactionService : IDiagnosticsRedactionService
{
    public string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var result = value;
        result = JwtRegex().Replace(result, "[REDACTED_JWT]");
        result = SecretPairRegex().Replace(result, "$1=[REDACTED]");
        result = BearerRegex().Replace(result, "Bearer [REDACTED]");
        result = CookieRegex().Replace(result, "$1=[REDACTED]");
        result = ConnectionStringPasswordRegex().Replace(result, "$1=[REDACTED]");
        result = LineUserRegex().Replace(result, match => MaskLineUserId(match.Value));
        result = EmailRegex().Replace(result, match => MaskEmail(match.Value));
        result = PhoneRegex().Replace(result, "[REDACTED_PHONE]");
        result = ThaiCitizenIdRegex().Replace(result, "[REDACTED_CID]");
        return result.Replace("\0", string.Empty);
    }

    private static string MaskLineUserId(string value)
    {
        return value.Length <= 10 ? "U***" : $"{value[..5]}...{value[^4..]}";
    }

    private static string MaskEmail(string value)
    {
        var at = value.IndexOf('@');
        if (at <= 1)
        {
            return "***@***";
        }

        return $"{value[0]}***{value[at..]}";
    }

    [GeneratedRegex(@"eyJ[a-zA-Z0-9_\-]+\.[a-zA-Z0-9_\-]+\.[a-zA-Z0-9_\-]+")]
    private static partial Regex JwtRegex();

    [GeneratedRegex(@"(?i)\b(password|passwd|pwd|secret|token|access_token|refresh_token|jwt|authorization|connectionstring|connection string|apikey|api_key)\s*[:=]\s*[^;\s,""'}]+")]
    private static partial Regex SecretPairRegex();

    [GeneratedRegex(@"(?i)Bearer\s+[a-zA-Z0-9_\-\.=]+")]
    private static partial Regex BearerRegex();

    [GeneratedRegex(@"(?i)\b(cookie|set-cookie)\s*[:=]\s*[^;\r\n]+")]
    private static partial Regex CookieRegex();

    [GeneratedRegex(@"(?i)\b(Password|Pwd)\s*=\s*[^;]+")]
    private static partial Regex ConnectionStringPasswordRegex();

    [GeneratedRegex(@"U[a-fA-F0-9]{31,}")]
    private static partial Regex LineUserRegex();

    [GeneratedRegex(@"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?<!\d)(?:\+?66|0)\d{8,9}(?!\d)")]
    private static partial Regex PhoneRegex();

    [GeneratedRegex(@"(?<!\d)\d{13}(?!\d)")]
    private static partial Regex ThaiCitizenIdRegex();
}
