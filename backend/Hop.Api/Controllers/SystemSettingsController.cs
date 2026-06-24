using Hop.Api.Authorization;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hop.Api.Controllers;

[ApiController]
[Route("api/system-settings")]
[Authorize]
public class SystemSettingsController(IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    [RequirePermission("SystemSettings.View")]
    public ActionResult<ApiResponse<SystemSettingsResponse>> GetSettings()
    {
        var response = new SystemSettingsResponse(
            configuration["Hospital:Name"] ?? configuration["HOSPITAL_NAME"] ?? "Hospital Operations Portal",
            configuration["Hospital:LogoPath"] ?? configuration["HOSPITAL_LOGO_PATH"] ?? "/assets/logo/hospital-logo.png",
            configuration["Footer:Text"] ?? configuration["FOOTER_TEXT"] ?? "Hospital Operations Portal",
            configuration["Footer:Developer"] ?? configuration["FOOTER_DEVELOPER"] ?? "งานเทคโนโลยีสารสนเทศ",
            configuration["Theme:PrimaryColor"] ?? configuration["THEME_PRIMARY_COLOR"] ?? "#2E5E4E",
            configuration["Theme:SecondaryColor"] ?? configuration["THEME_SECONDARY_COLOR"] ?? "#8B6B4A",
            configuration["Application:Version"] ?? configuration["APP_VERSION"] ?? "0.1.0",
            configuration["LeavePdf:TemplateConfigPath"] ?? configuration["LEAVE_PDF_TEMPLATE_CONFIG_PATH"] ?? "storage/templates/leave/leave_form_template.json",
            configuration["LeavePdf:FontPath"] ?? configuration["LEAVE_PDF_FONT_PATH"] ?? "assets/fonts/THSarabunPSK.ttf",
            configuration["LeavePdf:FontFamily"] ?? configuration["LEAVE_PDF_FONT_FAMILY"] ?? "TH SarabunPSK",
            configuration.GetValue("LeavePdf:FontSize", configuration.GetValue("LEAVE_PDF_FONT_SIZE", 16)),
            configuration.GetValue("LeavePdf:LineHeight", configuration.GetValue("LEAVE_PDF_LINE_HEIGHT", 1.2)),
            configuration.GetValue("LINE:Enabled", configuration.GetValue("LINE_ENABLED", false)),
            !string.IsNullOrWhiteSpace(configuration["LINE:ChannelAccessToken"] ?? configuration["LINE_CHANNEL_ACCESS_TOKEN"]),
            configuration["LINE:Endpoint"] ?? configuration["LINE_ENDPOINT"] ?? "https://api.line.me/v2/bot/message/push"
        );

        return ApiResponse<SystemSettingsResponse>.Ok(response);
    }
}
