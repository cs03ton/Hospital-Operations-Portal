using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetLineGroupMessageTemplateTests
{
    [Theory]
    [InlineData("Fleet.RequestSubmitted")]
    [InlineData("Fleet.AssignmentCreated")]
    [InlineData("Fleet.AdminReviewed")]
    [InlineData("Fleet.Returned")]
    [InlineData("Fleet.DirectorApproved")]
    [InlineData("Fleet.Rejected")]
    [InlineData("Fleet.Cancelled")]
    [InlineData("Fleet.AssignmentChanged")]
    [InlineData("Fleet.DriverAcknowledged")]
    [InlineData("Fleet.TripCompleted")]
    [InlineData("Fleet.TripOverdue")]
    public async Task Every_supported_canonical_event_has_a_thai_text_template(string canonicalEvent)
    {
        await using var db = Database();
        var fixture = await Seed(db, withAssignment: true);

        var result = await Template(db).RenderAsync(Event(fixture.Request.Id, fixture.Actor.Id, canonicalEvent), canonicalEvent, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("text", result!.Format);
        Assert.Contains("เลขที่:", result.Text);
        Assert.Contains("สถานะ:", result.Text);
    }

    [Fact]
    public void Mapper_is_exact_fleet_only_and_never_maps_leave_or_wildcards()
    {
        var mapper = new FleetLineGroupEventMapper();

        Assert.Equal("Fleet.RequestSubmitted", mapper.ToCanonical("FLEET", "Fleet.RequestSubmitted"));
        Assert.Equal("Fleet.AssignmentCreated", mapper.ToCanonical("FLEET", "Fleet.VehicleAssigned"));
        Assert.Equal("Fleet.AssignmentChanged", mapper.ToCanonical("FLEET", "Fleet.AssignmentReplaced"));
        Assert.Null(mapper.ToCanonical("LEAVE", "Fleet.RequestSubmitted"));
        Assert.Null(mapper.ToCanonical("LEAVE", "LeaveSubmitted"));
        Assert.Null(mapper.ToCanonical("FLEET", "Fleet.Unknown"));
        Assert.Null(mapper.ToCanonical("FLEET", "Fleet.*"));
    }

    [Fact]
    public async Task Director_approved_template_contains_required_assignment_data_and_secure_deep_link()
    {
        await using var db = Database();
        var fixture = await Seed(db, withAssignment: true);
        var service = Template(db);
        var domainEvent = Event(fixture.Request.Id, fixture.Actor.Id, "Fleet.DirectorApproved");

        var result = await service.RenderAsync(domainEvent, "Fleet.DirectorApproved", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("text", result!.Format);
        Assert.Contains("VH-202608-0001", result.Text);
        Assert.Contains("Requester One", result.Text);
        Assert.Contains("งานบริหาร", result.Text);
        Assert.Contains("05/08/2569 15:30", result.Text);
        Assert.Contains("โรงพยาบาลจังหวัด", result.Text);
        Assert.Contains("3 คน", result.Text);
        Assert.Contains("VH-01 (กข 1234)", result.Text);
        Assert.Contains("Driver One", result.Text);
        Assert.Contains("Director One", result.Text);
        Assert.Contains($"https://hop.example/fleet/requests/{fixture.Request.Id}", result.Text);
    }

    [Fact]
    public async Task Assignment_changed_template_shows_previous_and_new_assignment()
    {
        await using var db = Database();
        var fixture = await Seed(db, withAssignment: true, withReplacement: true);
        var payload = $$"""{"PreviousAssignmentId":"{{fixture.Previous!.Id}}","NewAssignmentId":"{{fixture.Current!.Id}}"}""";
        var domainEvent = Event(fixture.Request.Id, fixture.Actor.Id, "Fleet.AssignmentReplaced", payload);

        var result = await Template(db).RenderAsync(domainEvent, "Fleet.AssignmentChanged", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("เดิม: VH-OLD (กค 1111) · Driver Old", result!.Text);
        Assert.Contains("ใหม่: VH-01 (กข 1234) · Driver One", result.Text);
    }

    [Fact]
    public async Task Trip_completed_template_uses_trip_assignment_after_assignment_is_closed()
    {
        await using var db = Database();
        var fixture = await Seed(db, withAssignment: true);
        fixture.Current!.IsActive = false;
        fixture.Current.AssignmentStatus = FleetAssignmentStatuses.Completed;
        db.FleetTripRecords.Add(new FleetTripRecord
        {
            FleetRequestId = fixture.Request.Id,
            AssignmentId = fixture.Current.Id,
            DriverUserId = fixture.Current.DriverUserId,
            ActualStartAt = DateTime.UtcNow.AddHours(-1),
            ActualEndAt = DateTime.UtcNow,
            StartMileage = 100,
            EndMileage = 120
        });
        await db.SaveChangesAsync();

        var result = await Template(db).RenderAsync(
            Event(fixture.Request.Id, fixture.Actor.Id, "Fleet.TripCompleted"),
            "Fleet.TripCompleted",
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("VH-01 (กข 1234) · Driver One", result!.Text);
        using var flex = System.Text.Json.JsonDocument.Parse(result.FlexContentsJson!);
        Assert.Contains("Driver One", ExtractText(flex.RootElement));
        Assert.DoesNotContain("ยังไม่ได้จัดรถและคนขับ", result.FlexContentsJson);
    }

    private static string ExtractText(System.Text.Json.JsonElement element) => element.ValueKind switch
    {
        System.Text.Json.JsonValueKind.Object => string.Join(" ", element.EnumerateObject().Select(x => ExtractText(x.Value))),
        System.Text.Json.JsonValueKind.Array => string.Join(" ", element.EnumerateArray().Select(ExtractText)),
        System.Text.Json.JsonValueKind.String => element.GetString() ?? string.Empty,
        _ => string.Empty
    };

    [Fact]
    public async Task Template_has_assignment_fallback_and_does_not_leak_sensitive_fields()
    {
        await using var db = Database();
        var fixture = await Seed(db, withAssignment: false);
        db.FleetDriverProfiles.Add(new FleetDriverProfile
        {
            UserId = fixture.Actor.Id,
            LicenseNumber = "LICENSE-SECRET-9988",
            LicenseType = "PRIVATE",
            Note = "ลาป่วยเพราะข้อมูลสุขภาพ"
        });
        await db.SaveChangesAsync();

        var result = await Template(db).RenderAsync(Event(fixture.Request.Id, fixture.Actor.Id, "Fleet.DirectorApproved"), "Fleet.DirectorApproved", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("ยังไม่ระบุ", result!.Text);
        Assert.DoesNotContain("LICENSE-SECRET", result.Text);
        Assert.DoesNotContain("ลาป่วย", result.Text);
        Assert.DoesNotContain("ข้อมูลสุขภาพ", result.Text);
        Assert.DoesNotContain(fixture.Request.ContactPhone, result.Text);
    }

    [Fact]
    public async Task Unsupported_or_leave_event_does_not_render()
    {
        await using var db = Database();
        var fixture = await Seed(db, withAssignment: false);
        var service = Template(db);

        Assert.Null(await service.RenderAsync(Event(fixture.Request.Id, fixture.Actor.Id, "LeaveSubmitted", scope: "LEAVE"), "Fleet.RequestSubmitted", CancellationToken.None));
        Assert.Null(await service.RenderAsync(Event(fixture.Request.Id, fixture.Actor.Id, "Fleet.Unknown"), "Fleet.Unknown", CancellationToken.None));
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"fleet-group-template-{Guid.NewGuid()}").Options);

    private static FleetLineGroupMessageTemplateService Template(AppDbContext db)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Line:PublicAppUrl"] = "https://hop.example"
        }).Build();
        var resolver = new LineConfigurationResolver(Options.Create(new LineOptions { PublicAppUrl = "https://hop.example" }), configuration);
        return new FleetLineGroupMessageTemplateService(db, resolver);
    }

    private static DomainEventRecord Event(Guid requestId, Guid actorId, string eventType, string payload = "{}", string scope = "FLEET") => new()
    {
        EventId = Guid.NewGuid(), AggregateId = requestId, AggregateType = "FleetRequest", ActorUserId = actorId,
        EventType = eventType, Scope = scope, CorrelationId = "m2-test", Payload = payload
    };

    private static async Task<Fixture> Seed(AppDbContext db, bool withAssignment, bool withReplacement = false)
    {
        var department = new Department { Id = Guid.NewGuid(), Name = "งานบริหาร" };
        var requester = User("requester", "Requester One", department);
        var actor = User("director", "Director One", department);
        var driver = User("driver", "Driver One", department);
        var request = new FleetRequest
        {
            Id = Guid.NewGuid(), RequestNo = "VH-202608-0001", RequesterUser = requester, RequesterDepartment = department,
            RequesterUserId = requester.Id, RequesterDepartmentId = department.Id, RequestDate = new DateOnly(2026, 8, 5),
            Purpose = "ประชุม", MissionType = "ราชการ", Destination = "โรงพยาบาลจังหวัด", ContactPersonName = "ผู้ประสานงาน",
            ContactPhone = "0899999999", DepartureAt = new DateTime(2026, 8, 5, 8, 30, 0, DateTimeKind.Utc),
            ExpectedReturnAt = new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc), PassengerCount = 3,
            Status = FleetRequestStatuses.PendingDirector, CreatedByUserId = requester.Id
        };
        FleetAssignment? current = null; FleetAssignment? previous = null;
        if (withAssignment)
        {
            var type = new FleetVehicleType { Id = Guid.NewGuid(), Code = "VAN", Name = "รถตู้" };
            var vehicle = new FleetVehicle { Id = Guid.NewGuid(), VehicleCode = "VH-01", RegistrationNumber = "กข 1234", VehicleType = type, VehicleTypeId = type.Id, PassengerCapacity = 10, SeatCapacityTotal = 11 };
            current = new FleetAssignment { Id = Guid.NewGuid(), FleetRequest = request, FleetRequestId = request.Id, Vehicle = vehicle, VehicleId = vehicle.Id, DriverUser = driver, DriverUserId = driver.Id, AssignedByUserId = actor.Id, IsActive = true };
            request.Assignments.Add(current);
            db.AddRange(type, vehicle);
            if (withReplacement)
            {
                var oldDriver = User("old-driver", "Driver Old", department);
                var oldVehicle = new FleetVehicle { Id = Guid.NewGuid(), VehicleCode = "VH-OLD", RegistrationNumber = "กค 1111", VehicleType = type, VehicleTypeId = type.Id, PassengerCapacity = 3, SeatCapacityTotal = 4 };
                previous = new FleetAssignment { Id = Guid.NewGuid(), FleetRequest = request, FleetRequestId = request.Id, Vehicle = oldVehicle, VehicleId = oldVehicle.Id, DriverUser = oldDriver, DriverUserId = oldDriver.Id, AssignedByUserId = actor.Id, IsActive = false, AssignmentStatus = FleetAssignmentStatuses.Replaced };
                current.ReplacedAssignmentId = previous.Id;
                request.Assignments.Add(previous);
                db.AddRange(oldDriver, oldVehicle);
            }
        }
        db.AddRange(department, requester, actor, driver, request);
        await db.SaveChangesAsync();
        return new Fixture(request, actor, previous, current);
    }

    private static User User(string username, string name, Department department) => new()
    {
        Id = Guid.NewGuid(), Username = username, FullName = name, PasswordHash = "hash", Department = department, DepartmentId = department.Id
    };

    private sealed record Fixture(FleetRequest Request, User Actor, FleetAssignment? Previous, FleetAssignment? Current);
}
