using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Hop.Api.Configuration;

public sealed class LineConfigurationResolver(
    IOptions<LineOptions> options,
    IConfiguration configuration)
{
    private readonly LineOptions lineOptions = options.Value;

    public LineConfigurationResolver(
        IOptions<LineOptions> options,
        IConfiguration configuration,
        IHostEnvironment? _)
        : this(options, configuration)
    {
    }

    public bool Enabled =>
        configuration.GetValue<bool?>("Line:Enabled")
        ?? configuration.GetValue<bool?>("LINE:Enabled")
        ?? configuration.GetValue<bool?>("LINE_ENABLED")
        ?? lineOptions.Enabled;

    public string? ChannelId => FirstConfigured(
        lineOptions.ChannelId,
        configuration["Line:ChannelId"],
        configuration["LINE:ChannelId"],
        configuration["LINE_CHANNEL_ID"]);

    public string? ChannelSecret => FirstConfigured(
        lineOptions.ChannelSecret,
        configuration["Line:ChannelSecret"],
        configuration["LINE:ChannelSecret"],
        configuration["LINE_CHANNEL_SECRET"]);

    public string? AccessToken => FirstConfigured(
        lineOptions.AccessToken,
        lineOptions.ChannelAccessToken,
        configuration["Line:AccessToken"],
        configuration["Line:ChannelAccessToken"],
        configuration["LINE:AccessToken"],
        configuration["LINE:ChannelAccessToken"],
        configuration["LINE_ACCESS_TOKEN"],
        configuration["LINE_CHANNEL_ACCESS_TOKEN"]);

    public string? TestUserId => FirstConfigured(
        lineOptions.TestUserId,
        configuration["Line:TestUserId"],
        configuration["LINE:TestUserId"],
        configuration["LINE_TEST_USER_ID"]);

    public string Endpoint => FirstConfigured(
        lineOptions.Endpoint,
        configuration["Line:Endpoint"],
        configuration["LINE:Endpoint"],
        configuration["LINE_ENDPOINT"])
        ?? "https://api.line.me/v2/bot/message/push";

    public string? WebhookUrl => FirstConfigured(
        lineOptions.WebhookUrl,
        configuration["Line:WebhookUrl"],
        configuration["LINE:WebhookUrl"],
        configuration["LINE_WEBHOOK_URL"]);

    public string PublicAppUrl => FirstConfigured(
        lineOptions.PublicAppUrl,
        configuration["Line:PublicAppUrl"],
        configuration["PUBLIC_APP_URL"])
        ?? "http://localhost:5173";

    public string? PublicFileBaseUrl => FirstConfigured(
        lineOptions.PublicFileBaseUrl,
        configuration["Line:PublicFileBaseUrl"],
        configuration["Storage:PublicBaseUrl"],
        configuration["PUBLIC_FILE_BASE_URL"],
        configuration["STORAGE_PUBLIC_BASE_URL"]);

    public string? OaAddFriendUrl => FirstConfigured(
        lineOptions.OaAddFriendUrl,
        configuration["Line:OaAddFriendUrl"],
        configuration["LINE_OA_ADD_FRIEND_URL"]);

    public bool LiffEnabled =>
        configuration.GetValue<bool?>("Line:LiffEnabled")
        ?? configuration.GetValue<bool?>("LINE_LIFF_ENABLED")
        ?? lineOptions.LiffEnabled;

    public string? LiffId => FirstConfigured(
        lineOptions.LiffId,
        configuration["Line:LiffId"],
        configuration["LINE_LIFF_ID"]);

    public bool HasChannelSecret => !string.IsNullOrWhiteSpace(ChannelSecret);

    public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);

    private static string? FirstConfigured(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
