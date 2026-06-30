using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface IUserAvatarUrlResolver
{
    UserAvatarInfo ResolveForLine(User? user);
}

public record UserAvatarInfo(
    string? ImageUrl,
    string Initials,
    bool HasImage,
    string AvatarMode,
    string? FallbackReason
);
