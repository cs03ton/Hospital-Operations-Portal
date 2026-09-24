using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Interfaces;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetMasterDataManagementTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Deactivation_BlocksOpenAssignment_ButAllowsCompletedHistory(bool vehicle)
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var driver = new User { Id = Guid.NewGuid(), Username = "driver", FullName = "Driver" };
        var car = new FleetVehicle { Id = Guid.NewGuid(), VehicleCode = "CAR-1", RegistrationNumber = "TEST-1" };
        var profile = new FleetDriverProfile { Id = Guid.NewGuid(), UserId = driver.Id, LicenseNumber = "123", LicenseType = "CAR" };
        var request = new FleetRequest { Id = Guid.NewGuid(), RequestNo = "VH-TEST", Status = FleetRequestStatuses.Ready };
        var assignment = new FleetAssignment { FleetRequestId = request.Id, VehicleId = car.Id, DriverUserId = driver.Id, AssignedByUserId = driver.Id };
        db.AddRange(driver, car, profile, request, assignment);
        await db.SaveChangesAsync();
        var controller = new FleetMasterDataController(db, new NoopAudit()) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var blocked = vehicle ? await controller.SetVehicleActive(car.Id, new(false), default) : await controller.SetDriverActive(profile.Id, new(false), default);
        Assert.IsType<ConflictObjectResult>(blocked.Result);
        Assert.True(vehicle ? car.IsActive : profile.IsActive);

        request.Status = FleetRequestStatuses.Completed;
        await db.SaveChangesAsync();
        var allowed = vehicle ? await controller.SetVehicleActive(car.Id, new(false), default) : await controller.SetDriverActive(profile.Id, new(false), default);
        Assert.NotNull(allowed.Value);
        Assert.False(vehicle ? car.IsActive : profile.IsActive);
        Assert.NotNull(await db.FleetAssignments.SingleOrDefaultAsync(x => x.Id == assignment.Id));
    }

    private sealed class NoopAudit : IAuditLogService
    {
        public Task WriteAsync(Guid? userId, string action, string resource, string? resourceId, string? detail, string result = "Success", HttpContext? httpContext = null) => Task.CompletedTask;
    }
}
