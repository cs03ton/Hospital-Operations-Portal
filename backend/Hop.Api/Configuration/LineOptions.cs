namespace Hop.Api.Configuration;

public sealed class LineOptions
{
    public bool Enabled { get; set; }
    public string? ChannelId { get; set; }
    public string? ChannelSecret { get; set; }
    public string? AccessToken { get; set; }
    public string? ChannelAccessToken { get; set; }
    public string? TestUserId { get; set; }
    public string? Endpoint { get; set; }
    public string? WebhookUrl { get; set; }
    public string? PublicAppUrl { get; set; }
    public string? PublicFileBaseUrl { get; set; }
    public string? OaAddFriendUrl { get; set; }
    public bool LiffEnabled { get; set; }
    public string? LiffId { get; set; }
    public string? LoginChannelId { get; set; }
    public string? LoginChannelSecret { get; set; }
    public string? IdTokenVerifyUrl { get; set; }
    public string? LiffBaseUrl { get; set; }
}
