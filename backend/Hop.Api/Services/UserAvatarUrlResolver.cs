using System.Net;
using Hop.Api.Configuration;
using Hop.Api.Interfaces;
using Hop.Api.Models;

namespace Hop.Api.Services;

public sealed class UserAvatarUrlResolver(LineConfigurationResolver lineConfiguration) : IUserAvatarUrlResolver
{
    public UserAvatarInfo ResolveForLine(User? user)
    {
        var initials = BuildInitials(user);
        if (user is null)
        {
            return new UserAvatarInfo(null, initials, false, "Placeholder", "NoUser");
        }

        if (string.IsNullOrWhiteSpace(user.ProfileImagePath))
        {
            return new UserAvatarInfo(null, initials, false, "Initials", "NoImage");
        }

        var baseUrl = lineConfiguration.PublicFileBaseUrl ?? lineConfiguration.PublicAppUrl;
        if (!IsPublicHttpUrl(baseUrl, out var reason))
        {
            return new UserAvatarInfo(null, initials, true, "Initials", reason);
        }

        var version = user.ProfileImageUpdatedAt?.Ticks ?? user.UpdatedAt?.Ticks ?? DateTime.UtcNow.Ticks;
        var imageUrl = $"{baseUrl!.TrimEnd('/')}/api/users/{user.Id}/profile-image?v={version}";
        return new UserAvatarInfo(imageUrl, initials, true, "ProfileImage", null);
    }

    private static bool IsPublicHttpUrl(string? value, out string reason)
    {
        reason = "InvalidUrl";
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            reason = "InvalidScheme";
            return false;
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase))
        {
            reason = "LocalhostUrl";
            return false;
        }

        if (IPAddress.TryParse(uri.Host, out var address) && IsPrivateAddress(address))
        {
            reason = "PrivateNetworkUrl";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            return bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168);
        }

        return address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6UniqueLocal;
    }

    private static string BuildInitials(User? user)
    {
        var name = !string.IsNullOrWhiteSpace(user?.FullName)
            ? user.FullName
            : !string.IsNullOrWhiteSpace(user?.Username)
                ? user.Username
                : "U";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "U";
        }

        if (parts.Length == 1)
        {
            return parts[0][0].ToString();
        }

        return $"{parts[0][0]}{parts[^1][0]}";
    }
}
