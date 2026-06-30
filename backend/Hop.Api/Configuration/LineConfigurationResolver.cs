using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Hop.Api.Configuration;

public sealed class LineConfigurationResolver(
    IOptions<LineOptions> options,
    IConfiguration configuration,
    IHostEnvironment? environment = null)
{
    private readonly LineOptions lineOptions = options.Value;
    private readonly string contentRootPath = environment?.ContentRootPath ?? AppContext.BaseDirectory;
    private readonly string environmentName = environment?.EnvironmentName ?? Environments.Production;

    public bool Enabled =>
        configuration.GetValue<bool?>("Line:Enabled")
        ?? configuration.GetValue<bool?>("LINE:Enabled")
        ?? configuration.GetValue<bool?>("LINE_ENABLED")
        ?? GetJsonBoolean("Enabled")
        ?? lineOptions.Enabled;

    public string? ChannelId => FirstConfigured(
        lineOptions.ChannelId,
        configuration["Line:ChannelId"],
        configuration["LINE:ChannelId"],
        configuration["LINE_CHANNEL_ID"],
        GetJsonLineValue("ChannelId"));

    public string? ChannelSecret => FirstConfigured(
        lineOptions.ChannelSecret,
        configuration["Line:ChannelSecret"],
        configuration["LINE:ChannelSecret"],
        configuration["LINE_CHANNEL_SECRET"],
        GetJsonLineValue("ChannelSecret"));

    public string? AccessToken => FirstConfigured(
        lineOptions.AccessToken,
        lineOptions.ChannelAccessToken,
        configuration["Line:AccessToken"],
        configuration["Line:ChannelAccessToken"],
        configuration["LINE:AccessToken"],
        configuration["LINE:ChannelAccessToken"],
        configuration["LINE_ACCESS_TOKEN"],
        configuration["LINE_CHANNEL_ACCESS_TOKEN"],
        GetJsonLineValue("AccessToken", "ChannelAccessToken"));

    public string? TestUserId => FirstConfigured(
        lineOptions.TestUserId,
        configuration["Line:TestUserId"],
        configuration["LINE:TestUserId"],
        configuration["LINE_TEST_USER_ID"],
        GetJsonLineValue("TestUserId"));

    public string Endpoint => FirstConfigured(
        lineOptions.Endpoint,
        configuration["Line:Endpoint"],
        configuration["LINE:Endpoint"],
        configuration["LINE_ENDPOINT"],
        GetJsonLineValue("Endpoint"))
        ?? "https://api.line.me/v2/bot/message/push";

    public string PublicAppUrl => FirstConfigured(
        lineOptions.PublicAppUrl,
        configuration["Line:PublicAppUrl"],
        configuration["PUBLIC_APP_URL"],
        GetJsonLineValue("PublicAppUrl"))
        ?? "http://localhost:5173";

    public string? PublicFileBaseUrl => FirstConfigured(
        lineOptions.PublicFileBaseUrl,
        configuration["Line:PublicFileBaseUrl"],
        configuration["PUBLIC_FILE_BASE_URL"],
        GetJsonLineValue("PublicFileBaseUrl"));

    public bool LiffEnabled =>
        configuration.GetValue<bool?>("Line:LiffEnabled")
        ?? configuration.GetValue<bool?>("LINE_LIFF_ENABLED")
        ?? GetJsonBoolean("LiffEnabled")
        ?? lineOptions.LiffEnabled;

    public string? LiffId => FirstConfigured(
        lineOptions.LiffId,
        configuration["Line:LiffId"],
        configuration["LINE_LIFF_ID"],
        GetJsonLineValue("LiffId"));

    public bool HasChannelSecret => !string.IsNullOrWhiteSpace(ChannelSecret);

    public bool HasAccessToken => !string.IsNullOrWhiteSpace(AccessToken);

    private static string? FirstConfigured(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private string? GetJsonLineValue(params string[] keys)
    {
        foreach (var path in GetAppSettingsPaths())
        {
            var value = ReadLineJsonValue(path, keys);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private bool? GetJsonBoolean(string key)
    {
        foreach (var path in GetAppSettingsPaths())
        {
            var value = ReadLineJsonValue(path, key);
            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private IEnumerable<string> GetAppSettingsPaths()
    {
        if (!string.IsNullOrWhiteSpace(environmentName))
        {
            yield return Path.Combine(contentRootPath, $"appsettings.{environmentName}.json");
        }

        yield return Path.Combine(contentRootPath, "appsettings.json");
    }

    private static string? ReadLineJsonValue(string path, params string[] keys)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (!document.RootElement.TryGetProperty("Line", out var lineSection))
            {
                return null;
            }

            foreach (var key in keys)
            {
                if (!lineSection.TryGetProperty(key, out var property))
                {
                    continue;
                }

                return property.ValueKind switch
                {
                    JsonValueKind.String => property.GetString(),
                    JsonValueKind.True => bool.TrueString,
                    JsonValueKind.False => bool.FalseString,
                    _ => property.ToString()
                };
            }
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }

        return null;
    }
}
