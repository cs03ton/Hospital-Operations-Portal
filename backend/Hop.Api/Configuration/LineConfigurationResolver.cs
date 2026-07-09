using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;

namespace Hop.Api.Configuration;

public sealed class LineConfigurationResolver(
    IOptions<LineOptions> options,
    IConfiguration configuration)
{
    private readonly LineOptions lineOptions = options.Value;
    private IHostEnvironment? hostEnvironment;

    public LineConfigurationResolver(
        IOptions<LineOptions> options,
        IConfiguration configuration,
        IHostEnvironment? _)
        : this(options, configuration)
    {
        hostEnvironment = _;
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

    public string PublicAppUrl => ResolvePublicAppUrl();

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

    private string ResolvePublicAppUrl()
    {
        var candidates = new[]
        {
            lineOptions.PublicAppUrl,
            configuration["Line:PublicAppUrl"],
            configuration["LINE:PublicAppUrl"],
            configuration["LINE_PUBLIC_APP_URL"],
            configuration["PUBLIC_APP_URL"],
            configuration["PHASE1_FRONTEND_URL"],
            configuration["FRONTEND_URL"]
        };

        var usableUrl = candidates
            .Select(NormalizeUrl)
            .FirstOrDefault(IsUsableLineActionUrl);

        if (!string.IsNullOrWhiteSpace(usableUrl))
        {
            return usableUrl;
        }

        var configuredUrl = NormalizeUrl(FirstConfigured(candidates));
        if (!string.IsNullOrWhiteSpace(configuredUrl) && IsDevelopmentEnvironment())
        {
            return configuredUrl;
        }

        return IsDevelopmentEnvironment() ? "http://localhost:5173" : string.Empty;
    }

    private bool IsDevelopmentEnvironment()
    {
        return hostEnvironment?.IsDevelopment() == true ||
            string.Equals(configuration["ASPNETCORE_ENVIRONMENT"], Environments.Development, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeUrl(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().TrimEnd('/');
    }

    private static bool IsUsableLineActionUrl(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }
}
