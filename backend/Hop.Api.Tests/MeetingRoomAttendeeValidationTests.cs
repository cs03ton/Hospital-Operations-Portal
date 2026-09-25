using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class MeetingRoomAttendeeValidationTests
{
    [Fact]
    public async Task NamedAttendees_RequireTotalIncludingBooker_AndDeduplicateIds()
    {
        using var db = Database();
        var booker = User("Booker"); var employee = User("Employee");
        db.Users.AddRange(booker, employee); await db.SaveChangesAsync();
        Assert.Null(await MeetingRoomAttendeeValidation.ValidateAsync(db, [employee.Id, employee.Id], booker.Id, 2, default));
        Assert.NotNull(await MeetingRoomAttendeeValidation.ValidateAsync(db, [employee.Id], booker.Id, 1, default));
        Assert.Null(await MeetingRoomAttendeeValidation.ValidateAsync(db, [], booker.Id, 3, default));
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("SuperAdmin")]
    public async Task AdminRoles_AreExcludedFromBothPersonnelLists_AndMeetingSelection(string roleName)
    {
        using var db = Database();
        var employee = User("Employee"); var admin = User("Administrator");
        var role = new Role { Id = Guid.NewGuid(), Name = roleName };
        db.Users.AddRange(employee, admin); db.Roles.Add(role);
        db.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = role.Id });
        await db.SaveChangesAsync();

        var fleet = new FleetRequestsController(db, null!, null!, null!);
        var fleetResponse = await fleet.GetPersonnelOptions(ct: default);
        Assert.Contains(fleetResponse.Value!.Data!, x => x.Id == employee.Id);
        Assert.DoesNotContain(fleetResponse.Value!.Data!, x => x.Id == admin.Id);

        var meeting = new MeetingRoomsController(db, null!, null!, null!, null!, null!, null!);
        var meetingResponse = await meeting.PersonnelOptions(ct: default);
        var meetingData = (System.Collections.IEnumerable)meetingResponse.GetType().GetProperty("Data")!.GetValue(meetingResponse)!;
        var meetingIds = meetingData.Cast<object>().Select(x => (Guid)x.GetType().GetProperty("Id")!.GetValue(x)!).ToList();
        Assert.Contains(employee.Id, meetingIds);
        Assert.DoesNotContain(admin.Id, meetingIds);
        Assert.NotNull(await MeetingRoomAttendeeValidation.ValidateAsync(db, [admin.Id], employee.Id, 2, default));
        Assert.Null(await MeetingRoomAttendeeValidation.ValidateAsync(db, [], admin.Id, 1, default));
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static User User(string name) => new() { Id = Guid.NewGuid(), FullName = name, Username = name, IsActive = true };
}
