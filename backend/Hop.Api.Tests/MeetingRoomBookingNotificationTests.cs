using System.Security.Claims;
using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace Hop.Api.Tests;

public sealed class MeetingRoomBookingNotificationTests
{
    [Theory]
    [InlineData("https://hop.example.test", true)]
    [InlineData("", false)]
    [InlineData("http://hop.example.test", false)]
    public void BookingConfirmed_FlexContainsThaiBookingDetails_AndOptionalLink(string appUrl, bool hasLink)
    {
        var booking = new MeetingRoomBooking
        {
            Id = Guid.NewGuid(), Number = 42, Subject = "ประชุมคณะกรรมการ",
            StartAt = new DateTime(2026, 9, 25, 2, 0, 0, DateTimeKind.Utc),
            EndAt = new DateTime(2026, 9, 25, 3, 30, 0, DateTimeKind.Utc)
        };
        using var json = JsonDocument.Parse(MeetingRoomLineFlexMessageTemplates.BookingConfirmed(booking, "ห้องประชุมใหญ่", appUrl));
        var root = json.RootElement;
        Assert.Equal("", root.GetProperty("to").GetString());
        var message = Assert.Single(root.GetProperty("messages").EnumerateArray());
        Assert.Equal("flex", message.GetProperty("type").GetString());
        Assert.Contains("MR-000042", message.GetProperty("altText").GetString());
        var bubble = message.GetProperty("contents");
        Assert.Equal("bubble", bubble.GetProperty("type").GetString());
        var body = string.Join(" ", bubble.GetProperty("body").GetProperty("contents").EnumerateArray()
            .SelectMany(row => row.GetProperty("contents").EnumerateArray())
            .Select(text => text.GetProperty("text").GetString()));
        Assert.Contains("MR-000042", body);
        Assert.Contains("ห้องประชุมใหญ่", body);
        Assert.Contains("ประชุมคณะกรรมการ", body);
        Assert.Contains("25/09/2569 09:00 น.", body);
        Assert.Contains("25/09/2569 10:30 น.", body);
        Assert.Contains("ยืนยันการจองแล้ว", bubble.GetProperty("header").GetProperty("contents")[1].GetProperty("text").GetString());
        Assert.Equal(hasLink, bubble.TryGetProperty("footer", out var footer));
        if (hasLink)
            Assert.Equal($"https://hop.example.test/meeting-rooms/bookings/{booking.Id}",
                footer.GetProperty("contents")[0].GetProperty("action").GetProperty("uri").GetString());
    }

    [Fact]
    public async Task ConfirmedBooking_NotifiesBookerInAppAndLine_WithoutRollingBackWhenLineFails()
    {
        var adminConnection = Environment.GetEnvironmentVariable("HOP_MEETING_TEST_ADMIN_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(adminConnection)) return;
        var adminOptions = new NpgsqlConnectionStringBuilder(adminConnection);
        if (adminOptions.Host is not ("localhost" or "127.0.0.1"))
            throw new InvalidOperationException("Meeting integration tests require local PostgreSQL.");

        var databaseName = $"hop_meeting_test_{Guid.NewGuid():N}";
        var testOptions = new NpgsqlConnectionStringBuilder(adminConnection) { Database = databaseName };
        await using var admin = new NpgsqlConnection(adminConnection);
        await admin.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE {databaseName}", admin)) await create.ExecuteNonQueryAsync();
        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(testOptions.ConnectionString).Options;
            Guid bookerId;
            Guid roomId;
            await using (var setup = new AppDbContext(options))
            {
                await setup.Database.MigrateAsync();
                var booker = new User { Id = Guid.NewGuid(), Username = "booker", FullName = "ผู้จอง", IsActive = true };
                var room = new MeetingRoom { Id = Guid.NewGuid(), Code = "MR-TEST", Name = "ห้องประชุมทดสอบ", Capacity = 10 };
                setup.AddRange(booker, room);
                await setup.SaveChangesAsync();
                bookerId = booker.Id;
                roomId = room.Id;
            }

            var line = new CaptureLine();
            await using (var db = new AppDbContext(options))
            {
                var result = await Controller(db, bookerId, line).Create(Input(roomId, 10), default);
                Assert.IsType<OkObjectResult>(result);
            }
            Assert.Single(line.Messages);
            Assert.Equal(bookerId, line.Messages[0].UserId);
            Assert.Equal("MeetingRoom.BookingConfirmed", line.Messages[0].Event);
            Assert.Contains("https://hop.example.test/meeting-rooms/bookings/", line.Messages[0].Message);
            Assert.Contains("\"type\":\"flex\"", line.Messages[0].Message);

            await using (var db = new AppDbContext(options))
            {
                var result = await Controller(db, bookerId, line).Create(Input(roomId, 10), default);
                Assert.IsType<ConflictObjectResult>(result);
            }
            Assert.Single(line.Messages);

            line.ThrowOnSend = true;
            await using (var db = new AppDbContext(options))
            {
                var result = await Controller(db, bookerId, line).Create(Input(roomId, 12), default);
                Assert.IsType<OkObjectResult>(result);
            }

            await using var verify = new AppDbContext(options);
            var bookings = await verify.MeetingRoomBookings.OrderBy(x => x.StartAt).ToListAsync();
            Assert.Equal(2, bookings.Count);
            Assert.All(bookings, booking => Assert.Equal("Confirmed", booking.Status));
            var notices = await verify.Notifications.Where(x => x.Category == "MeetingRoom" && x.NotificationType == "BookingConfirmed").ToListAsync();
            Assert.Equal(2, notices.Count);
            Assert.All(notices, notice => Assert.Equal(bookerId, notice.UserId));
            Assert.Equal(2, notices.Select(x => x.ReferenceId).Distinct().Count());
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {databaseName} WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static MeetingBookingInput Input(Guid roomId, int hour) => new(roomId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), new TimeOnly(hour, 0), new TimeOnly(hour + 1, 0), "ประชุมทดสอบ", "ทดสอบแจ้งเตือน", 1, null, null);

    private static MeetingRoomsController Controller(AppDbContext db, Guid actor, CaptureLine line)
    {
        var configuration = new ConfigurationBuilder().Build();
        var resolver = new LineConfigurationResolver(Options.Create(new LineOptions { PublicAppUrl = "https://hop.example.test" }), configuration);
        var controller = new MeetingRoomsController(db, null!, null!, new CaptureEvents(), line, resolver, NullLogger<MeetingRoomsController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.ToString())], "Test"))
        } };
        return controller;
    }

    private sealed class CaptureEvents : IDomainEventPublisher
    {
        public Task PublishAsync(DomainEventEnvelope envelope, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class CaptureLine : ILineMessagingService
    {
        public List<(Guid UserId, string Event, string Message)> Messages { get; } = [];
        public bool ThrowOnSend { get; set; }
        public Task NotifyUserAsync(Guid userId, string eventName, string message, Guid? leaveRequestId = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();
        public Task<LineDeliveryLog> NotifyUserPayloadAsync(Guid userId, string eventName, string payload, Guid? leaveRequestId = null, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSend) throw new InvalidOperationException("LINE unavailable");
            Messages.Add((userId, eventName, payload));
            return Task.FromResult(new LineDeliveryLog { RecipientUserId = userId, EventName = eventName, Payload = payload });
        }
        public Task NotifyLeaveRequestAsync(LeaveNotificationMessage message, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LineTestSendResponse> SendTestMessageAsync(string toUserId, string message, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LineTestSendResponse> SendTestMessageAsync(string toUserId, string message, string eventName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LineTestSendResponse> SendRawPayloadToLineUserAsync(string toUserId, string payload, string eventName, Guid? leaveRequestId = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LineConnectionValidationResponse> ValidateConnectionAsync(IReadOnlyList<LineChecklistItemResponse> checklist, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> RetryPendingDeliveriesAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
