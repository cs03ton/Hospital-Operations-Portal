using System.Net;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Hop.Api.Tests;

public class LineRetryTests
{
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
            LineUserId = "U123"
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
            new HttpClient(new FakeLineHandler(HttpStatusCode.OK, "{}")),
            NullLogger<LineMessagingService>.Instance);

        var processed = await service.RetryPendingDeliveriesAsync();
        var log = await db.LineDeliveryLogs.SingleAsync();

        Assert.Equal(1, processed);
        Assert.Equal("Sent", log.Status);
        Assert.Equal(1, log.AttemptCount);
        Assert.Contains("\"to\":\"U123\"", log.Payload);
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
            LineUserId = "U123"
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
            new HttpClient(new FakeLineHandler(HttpStatusCode.OK, "{}")),
            NullLogger<LineMessagingService>.Instance);

        await service.RetryPendingDeliveriesAsync();
        var log = await db.LineDeliveryLogs.SingleAsync();

        Assert.Equal("Failed", log.Status);
        Assert.Contains("token", log.ResponseDetail, StringComparison.OrdinalIgnoreCase);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Line:ChannelAccessToken"] = "test-token",
                ["LineRetry:MaxAttempts"] = "3"
            })
            .Build();
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
