using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetDashboardM3Tests
{
    [Theory]
    [InlineData("2026-08-16T16:59:59Z", "2026-08-15T17:00:00Z", "2026-08-16T17:00:00Z", "2026-07-31T17:00:00Z")]
    [InlineData("2026-08-16T17:00:00Z", "2026-08-16T17:00:00Z", "2026-08-17T17:00:00Z", "2026-07-31T17:00:00Z")]
    public void Dashboard_date_ranges_follow_Asia_Bangkok(
        string nowText,
        string expectedDayStartText,
        string expectedDayEndText,
        string expectedMonthStartText)
    {
        var now = DateTime.Parse(nowText, null, System.Globalization.DateTimeStyles.AdjustToUniversal);

        var range = FleetDashboardService.GetBangkokDashboardRange(now);

        Assert.Equal(DateTime.Parse(expectedDayStartText, null, System.Globalization.DateTimeStyles.AdjustToUniversal), range.DayStartUtc);
        Assert.Equal(DateTime.Parse(expectedDayEndText, null, System.Globalization.DateTimeStyles.AdjustToUniversal), range.DayEndUtc);
        Assert.Equal(DateTime.Parse(expectedMonthStartText, null, System.Globalization.DateTimeStyles.AdjustToUniversal), range.MonthStartUtc);
    }

    [Fact]
    public async Task Requester_receives_only_own_request_section()
    {
        await using var db = Db();
        var requester = User("requester");
        var other = User("other");
        Grant(db, requester, FleetPermissions.RequestViewOwn);
        db.Add(other);
        db.FleetRequests.AddRange(Request(requester, "VH-OWN", FleetRequestStatuses.Draft), Request(other, "VH-OTHER", FleetRequestStatuses.PendingDispatch));
        await db.SaveChangesAsync();

        var result = await Service(db).GetAsync(requester.Id, default);

        Assert.NotNull(result);
        Assert.True(result.Capabilities.CanViewOwnRequests);
        Assert.NotNull(result.Requester);
        Assert.Equal(1, result.Requester.StatusCounts[FleetRequestStatuses.Draft]);
        Assert.Null(result.Dispatcher);
        Assert.Null(result.AdminReviewer);
        Assert.Equal(0, result.Badges.DispatchQueue);
    }

    [Fact]
    public async Task Driver_dashboard_counts_only_active_assignments_for_that_driver()
    {
        await using var db = Db();
        var driver = User("driver");
        var otherDriver = User("other-driver");
        var requester = User("requester");
        Grant(db, driver, FleetPermissions.DriverViewOwnJobs);
        db.AddRange(otherDriver, requester);
        var type = new FleetVehicleType { Code = "T", Name = "Type" };
        var vehicle = new FleetVehicle { VehicleCode = "V", RegistrationNumber = "1", VehicleType = type, PassengerCapacity = 3, SeatCapacityTotal = 4 };
        var own = Request(requester, "VH-D1", FleetRequestStatuses.Ready);
        var other = Request(requester, "VH-D2", FleetRequestStatuses.Ready);
        db.AddRange(type, vehicle, own, other,
            new FleetAssignment { FleetRequest = own, Vehicle = vehicle, DriverUser = driver, AssignedByUser = requester, IsActive = true },
            new FleetAssignment { FleetRequest = other, Vehicle = vehicle, DriverUser = otherDriver, AssignedByUser = requester, IsActive = true });
        await db.SaveChangesAsync();

        var result = await Service(db).GetAsync(driver.Id, default);

        Assert.NotNull(result?.Driver);
        Assert.Equal(1, result.Badges.MyDriverJobs);
        Assert.Null(result.Requester);
        Assert.Null(result.Dispatcher);
    }

    [Fact]
    public async Task Active_fleet_delegation_adds_effective_director_capability()
    {
        await using var db = Db();
        var director = User("director");
        var delegateUser = User("delegate");
        Grant(db, director, FleetPermissions.DirectorApprove);
        db.Add(delegateUser);
        var now = DateTime.UtcNow;
        db.ApprovalDelegations.Add(new ApprovalDelegation
        {
            ApproverUser = director, DelegateUser = delegateUser, Scope = "FLEET",
            RequiredPermissionCode = FleetPermissions.DirectorApprove,
            StartAt = now.AddHours(-1), EndAt = now.AddHours(1),
            StartDate = DateOnly.FromDateTime(now), EndDate = DateOnly.FromDateTime(now.AddDays(1)), Reason = "UAT"
        });
        await db.SaveChangesAsync();

        var result = await Service(db).GetAsync(delegateUser.Id, default);

        Assert.NotNull(result);
        Assert.True(result.Capabilities.CanDirectorApprove);
        Assert.Contains(FleetPermissions.DirectorApprove, result.Capabilities.DelegatedPermissions);
        Assert.NotNull(result.Director);
        Assert.Null(result.AdminReviewer);
    }

    [Fact]
    public async Task User_without_fleet_permission_is_rejected_by_service()
    {
        await using var db = Db();
        var user = User("none");
        db.Add(user);
        await db.SaveChangesAsync();

        Assert.Null(await Service(db).GetAsync(user.Id, default));
    }

    [Fact]
    public async Task Feedback_widget_is_returned_only_for_management_permission()
    {
        await using var db = Db();
        var manager = User("feedback-manager");
        var requester = User("requester");
        Grant(db, manager, FleetPermissions.FeedbackViewManagement);
        Grant(db, requester, FleetPermissions.RequestViewOwn);
        await db.SaveChangesAsync();

        var managerResult = await Service(db).GetAsync(manager.Id, default);
        var requesterResult = await Service(db).GetAsync(requester.Id, default);

        Assert.NotNull(managerResult?.Feedback);
        Assert.Equal(2, managerResult.Feedback.AttentionThreshold);
        Assert.Null(requesterResult?.Feedback);
    }

    private static FleetDashboardService Service(AppDbContext db) => new(
        db,
        new FleetEffectivePermissionService(db),
        Options.Create(new FleetOperationsOptions { FeedbackAttentionThreshold = 2 }));
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase($"fleet-dashboard-{Guid.NewGuid():N}").Options);
    private static User User(string name) => new() { Id = Guid.NewGuid(), Username = name, FullName = name, PasswordHash = "x" };

    private static void Grant(AppDbContext db, User user, string code)
    {
        var permission = new Permission { Code = code, Name = code, Group = "Fleet", Action = "View" };
        var role = new Role { Name = $"role-{user.Username}-{code}", RolePermissions = [new RolePermission { Permission = permission }] };
        user.UserRoles.Add(new UserRole { Role = role });
        db.Add(user);
    }

    private static FleetRequest Request(User requester, string no, string status) => new()
    {
        RequestNo = no, RequesterUser = requester, RequestDate = DateOnly.FromDateTime(DateTime.UtcNow), Purpose = "QA",
        MissionType = "QA", Destination = "QA", ContactPersonName = "QA", ContactPhone = "0", PassengerCount = 1,
        DepartureAt = DateTime.UtcNow.AddHours(2), ExpectedReturnAt = DateTime.UtcNow.AddHours(3), Status = status, CreatedByUserId = requester.Id
    };
}
