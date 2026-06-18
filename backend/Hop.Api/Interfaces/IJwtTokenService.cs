using Hop.Api.Models;

namespace Hop.Api.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, string roleName);
    string GenerateRefreshToken();
}
