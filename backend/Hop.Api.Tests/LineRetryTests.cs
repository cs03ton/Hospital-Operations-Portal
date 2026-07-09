using System.Net;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public class LineRetryTests
{
    private const string TestLineUserId = "U12345678901234567890";

    [Fact]
    public async Task SendTestMessageAsync_ReturnsDisabledWhenLineIsOff()
    {
        await using var db = CreateDbContext();
        var service = new LineMessagingService(
            db,
            CreateConfiguration(enabled: false),
            CreateLineResolver(CreateConfiguration(enabled: false)),
            new HttpClient(new FakeLineHandler(HttpStatusCode.OK, "{}")),
            NullLogger<LineMessagingService>.Instance);

        var result = await service.SendTestMessageAsync(TestLineUserId, "hello");

        Assert.False(result.Success);
        Assert.Equal("Disabled", result.DeliveryStatus);
        Assert.Empty(db.LineDeliveryLogs);
    }

    [Fact]
    public async Task SendTestMessageAsync_MarksSentWhenLineApiSucceeds()
    {
        await using var db = CreateDbContext();
        var service = new LineMessagingService(
            db,
            CreateConfiguration(enabled: true),
            CreateLineResolver(CreateConfiguration(enabled: true)),
            new HttpClient(new FakeLineHandler(HttpStatusCode.OK, "{}")),
            NullLogger<LineMessagingService>.Instance);

        var result = await service.SendTestMessageAsync(TestLineUserId, "hello");
        var log = await db.LineDeliveryLogs.SingleAsync();

        Assert.True(result.Success);
        Assert.Equal("Sent", log.Status);
        Assert.Equal(200, result.HttpStatusCode);
        Assert.NotNull(result.ResponseTimeMs);
        Assert.Contains($"\"to\":\"{TestLineUserId}\"", log.Payload);
    }

    [Fact]
    public async Task SendTestMessageAsync_MarksFailedWhenLineApiFails()
    {
        await using var db = CreateDbContext();
        var service = new LineMessagingService(
            db,
            CreateConfiguration(enabled: true),
            CreateLineResolver(CreateConfiguration(enabled: true)),
            new HttpClient(new FakeLineHandler(HttpStatusCode.BadRequest, "{\"message\":\"bad\"}")),
            NullLogger<LineMessagingService>.Instance);

        var result = await service.SendTestMessageAsync(TestLineUserId, "hello");
        var log = await db.LineDeliveryLogs.SingleAsync();

        Assert.False(result.Success);
        Assert.Equal("Failed", log.Status);
        Assert.Contains("HTTP 400", log.ResponseDetail);
    }

    [Fact]
    public async Task SendTestMessageAsync_ReturnsFailedWhenUserIdMissing()
    {
        await using var db = CreateDbContext();
        var service = new LineMessagingService(
            db,
            CreateConfiguration(enabled: true),
            CreateLineResolver(CreateConfiguration(enabled: true)),
            new HttpClient(new FakeLineHandler(HttpStatusCode.OK, "{}")),
            NullLogger<LineMessagingService>.Instance);

        var result = await service.SendTestMessageAsync("", "hello");

        Assert.False(result.Success);
        Assert.Equal("Failed", result.DeliveryStatus);
        Assert.Contains("LINE User ID", result.Message);
        Assert.Empty(db.LineDeliveryLogs);
    }

    [Fact]
    public async Task RetryPendingDeliveriesAsync_SendsQueuedDeliveryAndMarksSent()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Username = "line-user",
            FullName = "Line User",
            PasswordHash = "hash",
            LineUserId = TestLineUserId
        });
        db.LineDeliveryLogs.Add(new LineDeliveryLog
        {
            Id = Guid.NewGuid(),
            RecipientUserId = userId,
            EventName = "LeaveRequest.Pending",
            Status = "Queued",
            Payload = "{\"to\":\"\",\"messages\":[{\"type\":\"text\",\"text\":\"test\"}]}",
            NextRetryAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();
        var service = new LineMessagingService(
            db,
            CreateConfiguration(),
            CreateLineResolver(CreateConfiguration()),
            new HttpClient(new FakeLineHandler(HttpStatusCode.OK, "{}")),
            NullLogger<LineMessagingService>.Instance);

        var processed = await service.RetryPendingDeliveriesAsync();
        var log = await db.LineDeliveryLogs.SingleAsync();

        Assert.Equal(1, processed);
        Assert.Equal("Sent", log.Status);
        Assert.Equal(1, log.AttemptCount);
        Assert.Contains($"\"to\":\"{TestLineUserId}\"", log.Payload);
    }

    [Fact]
    public async Task RetryPendingDeliveriesAsync_MarksFailedWhenTokenMissing()
    {
        await using var db = CreateDbContext();
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            Username = "line-user",
            FullName = "Line User",
            PasswordHash = "hash",
            LineUserId = TestLineUserId
        });
        db.LineDeliveryLogs.Add(new LineDeliveryLog
        {
            Id = Guid.NewGuid(),
            RecipientUserId = userId,
            EventName = "LeaveRequest.Pending",
            Status = "Queued",
            Payload = "{\"to\":\"\",\"messages\":[{\"type\":\"text\",\"text\":\"test\"}]}",
            NextRetryAt = DateTime.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();
        var service = new LineMessagingService(
            db,
            new ConfigurationBuilder().Build(),
            CreateLineResolver(new ConfigurationBuilder().Build()),
            new HttpClient(new FakeLineHandler(HttpStatusCode.OK, "{}")),
            NullLogger<LineMessagingService>.Instance);

        await service.RetryPendingDeliveriesAsync();
        var log = await db.LineDeliveryLogs.SingleAsync();

        Assert.Equal("Failed", log.Status);
        Assert.Contains("token", log.ResponseDetail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LineConfigurationResolver_DoesNotReadAppSettingsFallbackWhenMergedConfigIsBlank()
    {
        var settingsDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(settingsDirectory);
        var jsonChannelSecret = $"line-secret-{Guid.NewGuid():N}";
        var jsonAccessToken = $"line-access-{Guid.NewGuid():N}";
        File.WriteAllText(
            Path.Combine(settingsDirectory, "appsettings.Development.json"),
            $$"""
            {
              "Line": {
                "Enabled": true,
                "ChannelSecret": "{{jsonChannelSecret}}",
                "ChannelAccessToken": "{{jsonAccessToken}}"
              }
            }
            """);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Line:ChannelSecret"] = string.Empty,
                ["Line:ChannelAccessToken"] = string.Empty
            })
            .Build();

        var resolver = new LineConfigurationResolver(
            Options.Create(new LineOptions()),
            configuration,
            new TestHostEnvironment(settingsDirectory, Environments.Development));

        Assert.False(resolver.HasChannelSecret);
        Assert.False(resolver.HasAccessToken);
        Assert.Null(resolver.ChannelSecret);
        Assert.Null(resolver.AccessToken);
    }

    [Fact]
    public void LeaveLineFlexMessageTemplates_BuildsApprovalCardWithThreeSecureUrls()
    {
        var leaveRequest = CreateLeaveRequest();

        var payload = LeaveLineFlexMessageTemplates.BuildPendingApprovalCard(leaveRequest, "https://hop.example.local");
        using var document = JsonDocument.Parse(payload);
        var text = payload;

        Assert.Equal("flex", document.RootElement.GetProperty("messages")[0].GetProperty("type").GetString());
        Assert.Contains("https://hop.example.local/leave/", text);
        Assert.Contains("https://hop.example.local/line/leave-approval/", text);
        Assert.Contains("action=approve", text);
        Assert.Contains("action=reject", text);
        Assert.Contains("LV-202606-001", text);
    }

    [Fact]
    public void LineConfigurationResolver_PublicAppUrlPrefersConfiguredNonLocalUrl()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = Environments.Production,
                ["Line:PublicAppUrl"] = "http://localhost:5173",
                ["PUBLIC_APP_URL"] = "http://172.16.2.99"
            })
            .Build();

        var resolver = new LineConfigurationResolver(
            Options.Create(new LineOptions()),
            configuration,
            new TestHostEnvironment(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), Environments.Production));

        Assert.Equal("http://172.16.2.99", resolver.PublicAppUrl);
    }

    [Fact]
    public void LeaveLineFlexMessageTemplates_PendingCardUsesMinimalActionColors()
    {
        var payload = LeaveLineFlexMessageTemplates.BuildPendingApprovalCard(CreateLeaveRequest(), "https://hop.example.local");
        using var document = JsonDocument.Parse(payload);
        var footerButtons = GetFooterButtons(document).ToList();

        Assert.Equal("flex", document.RootElement.GetProperty("messages")[0].GetProperty("type").GetString());
        Assert.Equal("bubble", document.RootElement.GetProperty("messages")[0].GetProperty("contents").GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("messages")[0].GetProperty("altText").GetString()));
        Assert.Equal(3, footerButtons.Count);
        Assert.Contains(footerButtons, item => item.Label == "ดูรายละเอียด" && item.Color == "#2563EB");
        Assert.Contains(footerButtons, item => item.Label == "อนุมัติ" && item.Color == "#16A34A");
        Assert.Contains(footerButtons, item => item.Label == "ไม่อนุมัติ" && item.Color == "#DC2626");
    }

    [Fact]
    public void LeaveLineFlexMessageTemplates_ApprovedCardDoesNotShowRejectButton()
    {
        var payload = LeaveLineFlexMessageTemplates.BuildApprovedCard(CreateLeaveRequest(), "https://hop.example.local");
        using var document = JsonDocument.Parse(payload);
        var footerButtons = GetFooterButtons(document).ToList();

        Assert.Single(footerButtons);
        Assert.Contains(footerButtons, item => item.Label == "ดูรายละเอียด" && item.Color == "#2563EB");
        Assert.DoesNotContain(footerButtons, item => item.Label == "ไม่อนุมัติ");
        Assert.Contains("อนุมัติแล้ว", GetMessageText(document));
    }

    [Fact]
    public void LeaveLineFlexMessageTemplates_RejectedCardDoesNotShowApprovalButtons()
    {
        var payload = LeaveLineFlexMessageTemplates.BuildRejectedCard(CreateLeaveRequest(), "https://hop.example.local");
        using var document = JsonDocument.Parse(payload);
        var footerButtons = GetFooterButtons(document).ToList();

        Assert.Single(footerButtons);
        Assert.Contains(footerButtons, item => item.Label == "ดูรายละเอียด" && item.Color == "#2563EB");
        Assert.DoesNotContain(footerButtons, item => item.Label == "อนุมัติ");
        Assert.DoesNotContain(footerButtons, item => item.Label == "ไม่อนุมัติ");
        Assert.Contains("ไม่อนุมัติ", GetMessageText(document));
    }

    [Fact]
    public void UserAvatarUrlResolver_UsesPublicFileBaseUrlForProfileImage()
    {
        var user = CreateLeaveRequest().User!;
        user.Id = Guid.NewGuid();
        user.ProfileImagePath = $"storage/profile-images/{user.Id:N}/avatar.webp";
        user.ProfileImageUpdatedAt = new DateTime(2026, 6, 30, 1, 2, 3, DateTimeKind.Utc);
        var resolver = CreateAvatarResolver("https://files.example.org");

        var avatar = resolver.ResolveForLine(user);
        var leaveRequest = CreateLeaveRequest();
        leaveRequest.User = user;
        var payload = LeaveLineFlexMessageTemplates.BuildPendingApprovalCard(leaveRequest, "https://hop.example.org", avatar);

        Assert.True(avatar.HasImage);
        Assert.Equal("ProfileImage", avatar.AvatarMode);
        Assert.StartsWith("https://files.example.org/api/users/", avatar.ImageUrl);
        Assert.Contains("\"type\":\"image\"", payload);
        Assert.Contains("files.example.org", payload);
    }

    [Fact]
    public void UserAvatarUrlResolver_FallsBackWhenUserHasNoProfileImage()
    {
        var user = CreateLeaveRequest().User!;
        var resolver = CreateAvatarResolver("https://files.example.org");

        var avatar = resolver.ResolveForLine(user);

        Assert.Null(avatar.ImageUrl);
        Assert.False(avatar.HasImage);
        Assert.Equal("Initials", avatar.AvatarMode);
        Assert.Equal("NoImage", avatar.FallbackReason);
        Assert.False(string.IsNullOrWhiteSpace(avatar.Initials));
    }

    [Theory]
    [InlineData("http://localhost:5000", "LocalhostUrl")]
    [InlineData("http://127.0.0.1:5000", "LocalhostUrl")]
    [InlineData("http://172.16.1.10", "PrivateNetworkUrl")]
    [InlineData("http://192.168.1.2", "PrivateNetworkUrl")]
    [InlineData("http://10.0.0.5", "PrivateNetworkUrl")]
    public void UserAvatarUrlResolver_FallsBackForNonPublicUrls(string fileBaseUrl, string reason)
    {
        var user = CreateLeaveRequest().User!;
        user.Id = Guid.NewGuid();
        user.ProfileImagePath = $"storage/profile-images/{user.Id:N}/avatar.webp";
        var resolver = CreateAvatarResolver(fileBaseUrl);

        var avatar = resolver.ResolveForLine(user);

        Assert.Null(avatar.ImageUrl);
        Assert.True(avatar.HasImage);
        Assert.Equal("Initials", avatar.AvatarMode);
        Assert.Equal(reason, avatar.FallbackReason);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfiguration(bool enabled = true)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Line:Enabled"] = enabled.ToString(),
                ["Line:ChannelAccessToken"] = $"line-access-{Guid.NewGuid():N}",
                ["LineRetry:MaxAttempts"] = "3"
            })
            .Build();
    }

    private static LineConfigurationResolver CreateLineResolver(IConfiguration configuration)
    {
        return new LineConfigurationResolver(
            Options.Create(new LineOptions()),
            configuration,
            new TestHostEnvironment(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), "Test"));
    }

    private static UserAvatarUrlResolver CreateAvatarResolver(string publicFileBaseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Line:PublicFileBaseUrl"] = publicFileBaseUrl,
                ["Line:PublicAppUrl"] = "https://hop.example.org"
            })
            .Build();
        return new UserAvatarUrlResolver(CreateLineResolver(configuration));
    }

    private static LeaveRequest CreateLeaveRequest()
    {
        return new LeaveRequest
        {
            Id = Guid.NewGuid(),
            RequestNumber = "LV-202606-001",
            StartDate = new DateOnly(2026, 7, 2),
            EndDate = new DateOnly(2026, 7, 2),
            DurationType = "FULL_DAY",
            TotalDays = 1,
            Reason = "ทดสอบระบบลา",
            User = new User
            {
                FullName = "เจ้าหน้าที่ 03",
                Department = new Department { Name = "Information Technology" }
            },
            LeaveType = new LeaveType { Name = "ลากิจ" },
            Approvals =
            [
                new LeaveApproval { StepOrder = 1, StepName = "หัวหน้าหน่วยงาน", Status = "Pending" }
            ]
        };
    }

    private static IEnumerable<(string Label, string Color)> GetFooterButtons(JsonDocument document)
    {
        var footerContents = document.RootElement
            .GetProperty("messages")[0]
            .GetProperty("contents")
            .GetProperty("footer")
            .GetProperty("contents");
        return GetButtons(footerContents);
    }

    private static IEnumerable<(string Label, string Color)> GetButtons(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("action", out var action))
            {
                yield return (
                    action.GetProperty("label").GetString() ?? string.Empty,
                    element.TryGetProperty("color", out var color) ? color.GetString() ?? string.Empty : string.Empty);
            }

            foreach (var property in element.EnumerateObject())
            {
                foreach (var button in GetButtons(property.Value))
                {
                    yield return button;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var button in GetButtons(item))
                {
                    yield return button;
                }
            }
        }
    }

    private static string GetMessageText(JsonDocument document)
    {
        var texts = new List<string>();
        CollectText(document.RootElement.GetProperty("messages")[0].GetProperty("contents"), texts);
        return string.Join(" ", texts);
    }

    private static void CollectText(JsonElement element, ICollection<string> texts)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("text", out var text))
            {
                texts.Add(text.GetString() ?? string.Empty);
            }

            foreach (var property in element.EnumerateObject())
            {
                CollectText(property.Value, texts);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectText(item, texts);
            }
        }
    }

    private sealed class TestHostEnvironment(string contentRootPath, string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Hop.Api.Tests";
        public string ContentRootPath { get; set; } = contentRootPath;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class FakeLineHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
