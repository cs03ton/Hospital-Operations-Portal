namespace Hop.Api.DTOs;

public record SystemSettingsResponse(
    string HospitalName,
    string HospitalLogoPath,
    string FooterText,
    string FooterDeveloper,
    string ThemePrimaryColor,
    string ThemeSecondaryColor,
    string ApplicationVersion,
    bool LineEnabled,
    bool LineChannelAccessTokenConfigured,
    string LineEndpoint
);
