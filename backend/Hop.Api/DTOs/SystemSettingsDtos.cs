namespace Hop.Api.DTOs;

public record SystemSettingsResponse(
    string HospitalName,
    string HospitalLogoPath,
    string FooterText,
    string FooterDeveloper,
    string ThemePrimaryColor,
    string ThemeSecondaryColor,
    string ApplicationVersion,
    string PdfTemplateConfigPath,
    string PdfFontPath,
    string PdfFontFamily,
    int PdfFontSize,
    double PdfLineHeight,
    bool LineEnabled,
    bool LineChannelAccessTokenConfigured,
    string LineEndpoint
);
