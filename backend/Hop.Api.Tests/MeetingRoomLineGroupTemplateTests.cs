using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class MeetingRoomLineGroupTemplateTests
{
    [Fact]
    public async Task Booking_flex_contains_safe_summary_and_excludes_private_fields()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"meeting-line-template-{Guid.NewGuid()}").Options);
        var department = new Department { Id = Guid.NewGuid(), Name = "Digital Health" };
        var user = new User { Id = Guid.NewGuid(), Username = "booker", FullName = "ผู้จองทดสอบ", PasswordHash = "hash", Department = department, DepartmentId = department.Id };
        var room = new MeetingRoom { Code = "R1", Name = "ห้องประชุมบริหาร", Location = "อาคาร 1", Capacity = 20 };
        var booking = new MeetingRoomBooking
        {
            RoomId = room.Id, BookerId = user.Id, DepartmentId = department.Id, Subject = "ประชุมแผนงาน",
            Purpose = "ข้อมูลวัตถุประสงค์ที่ไม่ควรส่ง", StartAt = DateTime.UtcNow.AddDays(1), EndAt = DateTime.UtcNow.AddDays(1).AddHours(1),
            AttendeeCount = 8, MeetingLink = "https://secret.example/meeting", AdditionalRequest = "อาหารว่างลับ"
        };
        db.AddRange(department, user, room, booking); await db.SaveChangesAsync();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Line:PublicAppUrl"] = "https://hop.example.go.th"
        }).Build();
        var resolver = new LineConfigurationResolver(Options.Create(new LineOptions { PublicAppUrl = "https://hop.example.go.th" }), configuration);
        var service = new FleetLineGroupMessageTemplateService(db, resolver);
        var message = await service.RenderAsync(new DomainEventRecord
        {
            EventId = Guid.NewGuid(), EventType = "MeetingRoom.BookingCreated", Scope = "MEETING_ROOM",
            AggregateType = nameof(MeetingRoomBooking), AggregateId = booking.Id
        }, "MeetingRoom.BookingCreated", CancellationToken.None);

        Assert.NotNull(message);
        Assert.Contains("MR-", message!.Text);
        Assert.Contains("ประชุมแผนงาน", message.Text);
        Assert.Contains("ห้องประชุมบริหาร", message.Text);
        Assert.Contains("ผู้จองทดสอบ", message.Text);
        Assert.DoesNotContain(booking.Purpose, message.Text);
        Assert.DoesNotContain(booking.MeetingLink, message.Text);
        Assert.DoesNotContain(booking.AdditionalRequest, message.Text);
        Assert.DoesNotContain(booking.Purpose, message.FlexContentsJson);
        Assert.Contains($"/meeting-rooms/bookings/{booking.Id}", message.FlexContentsJson);
    }
}
