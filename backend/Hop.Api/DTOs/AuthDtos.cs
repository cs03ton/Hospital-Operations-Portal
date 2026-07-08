namespace Hop.Api.DTOs;

public record LoginRequest(string Username, string Password);

public record RefreshTokenRequest(string? RefreshToken);

public record LogoutRequest(string? RefreshToken);

public record CsrfTokenResponse(
    string CookieName,
    string HeaderName
);

public record AuthUserDto(
    Guid Id,
    string Fullname,
    string Username,
    string Role,
    string? Department,
    string? ProfileImageUrl,
    IReadOnlyList<string> Permissions
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    AuthUserDto User
);
