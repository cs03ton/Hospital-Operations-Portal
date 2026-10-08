using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Hop.Api.Controllers;
using Hop.Api.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;

namespace Hop.Api.Tests;

public sealed class FleetReferPolicyTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    public async Task Assignment_only_auto_approves_refer_after_safety_checks(bool refer, bool urgent, bool blocked)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var driver = new User { Username = "driver", FullName = "Driver", IsActive = true };
        var vehicle = new FleetVehicle { VehicleCode = "AMB", VehicleType = new FleetVehicleType { Code = "AMBULANCE" }, Status = blocked ? FleetVehicleStatuses.Maintenance : FleetVehicleStatuses.Available };
        var request = new FleetRequest { RequesterUser = new User { Username = "requester", FullName = "Requester", Department = new Department { Name = "Test" } }, MissionType = refer ? FleetReferPolicy.MissionType : "ทั่วไป", IsUrgent = urgent,
            Status = FleetRequestStatuses.PendingDispatch, DepartureAt = DateTime.UtcNow.AddDays(1), ExpectedReturnAt = DateTime.UtcNow.AddDays(1).AddHours(2) };
        db.AddRange(vehicle, request, new FleetDriverProfile { User = driver, CanDriveAmbulance = true });
        await db.SaveChangesAsync();
        var actor = Guid.NewGuid();
        var controller = new FleetRequestsController(db, new FleetRequestNumberService(db), new FleetAvailabilityService(db), new DomainEventPublisher(db, new FleetNotificationRecipientResolver(db)))
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.ToString())], "Test")) } } };
        var result = await controller.Assign(request.Id, new FleetAssignRequest(vehicle.Id, driver.Id, request.ConcurrencyToken, null), default);
        if (blocked)
        {
            Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Empty(await db.DomainEvents.ToListAsync());
            Assert.Equal(FleetRequestStatuses.PendingDispatch, request.Status);
            return;
        }
        Assert.Null(result.Result);
        Assert.Equal(refer ? FleetRequestStatuses.PendingDriverAck : FleetRequestStatuses.PendingAdminReview, request.Status);
        var evt = Assert.Single(await db.DomainEvents.ToListAsync());
        Assert.Equal(refer ? "Fleet.ReferAutoApproved" : "Fleet.Assigned", evt.EventType);
        Assert.Equal(actor, evt.ActorUserId);
        Assert.Equal(evt.EventId, Assert.Single(await db.OutboxMessages.ToListAsync()).EventId);
        Assert.Equal(evt.EventId, Assert.Single(await db.FleetRequestStatusHistories.ToListAsync()).Id);
    }

    [Fact]
    public async Task Refer_overrides_spoofed_type_and_urgency()
    {
        await using var db = Database();
        var ambulance = new FleetVehicleType { Code = "AMBULANCE", Name = "รถพยาบาล" };
        db.Add(ambulance); await db.SaveChangesAsync();
        var request = new FleetRequest { MissionType = FleetReferPolicy.MissionType, RequestedVehicleTypeId = Guid.NewGuid(), IsUrgent = false };
        Assert.Null(await FleetReferPolicy.ApplyAsync(db, request, default));
        Assert.Equal(ambulance.Id, request.RequestedVehicleTypeId);
        Assert.True(request.IsUrgent);
        Assert.Equal(FleetPriorities.Urgent, request.Priority);
        Assert.Equal(FleetReferPolicy.MissionType, request.UrgentReason);
        request.UrgentReason = "รายละเอียดเดิม";
        Assert.Null(await FleetReferPolicy.ApplyAsync(db, request, default));
        Assert.Equal("รายละเอียดเดิม", request.UrgentReason);
    }

    [Fact]
    public async Task Missing_active_ambulance_type_is_a_clear_configuration_error()
    {
        await using var db = Database();
        db.Add(new FleetVehicleType { Code = "AMBULANCE", IsActive = false }); await db.SaveChangesAsync();
        Assert.Contains("ทะเบียนรถ", await FleetReferPolicy.ApplyAsync(db, new FleetRequest { MissionType = FleetReferPolicy.MissionType }, default));
    }

    [Fact]
    public async Task Availability_only_returns_ambulances_and_qualified_drivers_and_keeps_safety_checks()
    {
        await using var db = Database();
        var ambulance = new FleetVehicleType { Code = "AMBULANCE" };
        var van = new FleetVehicleType { Code = "VAN" };
        var request = new FleetRequest { Id = Guid.NewGuid(), MissionType = FleetReferPolicy.MissionType, DepartureAt = DateTime.UtcNow.AddDays(1), ExpectedReturnAt = DateTime.UtcNow.AddDays(1).AddHours(2) };
        for (var i = 0; i < 4; i++) db.Add(new FleetVehicle { VehicleCode = $"A{i}", VehicleType = ambulance, Status = i == 0 ? "MAINTENANCE" : FleetVehicleStatuses.Available });
        db.Add(new FleetVehicle { VehicleCode = "VAN", VehicleType = van });
        db.Add(new FleetDriverProfile { User = new User { Username = "ambulance-driver", FullName = "driver", IsActive = true }, CanDriveAmbulance = true });
        db.Add(new FleetDriverProfile { User = new User { Username = "van-driver", IsActive = true }, CanDriveAmbulance = false, CanDriveVan = true });
        await db.SaveChangesAsync();
        var result = await new FleetAvailabilityService(db).GetAsync(request, default);
        Assert.Equal(4, result.Vehicles.Count);
        Assert.All(result.Vehicles, x => Assert.StartsWith("A", x.Code));
        Assert.False(result.Vehicles.Single(x => x.Code == "A0").IsAvailable);
        Assert.Single(result.Drivers);
    }

    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
