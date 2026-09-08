using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetFeedbackM3Tests
{
    [Fact]
    public async Task TripAggregate_UsesActualEligibleEmployees_AndReturnsSixtyPercentWithoutIdentity()
    {
        await using var db = CreateDb();
        var manager = User("manager"); var driver = User("driver");
        var passengers = Enumerable.Range(1, 5).Select(x => User($"passenger{x}")).ToArray();
        var type = new FleetVehicleType { Id = Guid.NewGuid(), Code = "VAN", Name = "รถตู้" };
        var vehicle = new FleetVehicle { Id = Guid.NewGuid(), VehicleCode = "V-01", RegistrationNumber = "นข 1234", VehicleType = type, VehicleTypeId = type.Id, SeatCapacityTotal = 10, PassengerCapacity = 9, CreatedByUserId = manager.Id };
        var request = new FleetRequest { Id = Guid.NewGuid(), RequestNo = "VH-202608-0099", RequesterUserId = passengers[0].Id, Purpose = "ทดสอบ", MissionType = "GENERAL", Destination = "สสจ.น่าน", ContactPersonName = "ทดสอบ", ContactPhone = "1", DepartureAt = DateTime.UtcNow.AddDays(-1), ExpectedReturnAt = DateTime.UtcNow.AddDays(-1).AddHours(4), PassengerCount = 5, Status = FleetRequestStatuses.Completed, CreatedByUserId = manager.Id };
        var assignment = new FleetAssignment { Id = Guid.NewGuid(), FleetRequest = request, FleetRequestId = request.Id, Vehicle = vehicle, VehicleId = vehicle.Id, DriverUser = driver, DriverUserId = driver.Id, AssignedByUserId = manager.Id };
        var trip = new FleetTripRecord { Id = Guid.NewGuid(), FleetRequest = request, FleetRequestId = request.Id, Assignment = assignment, AssignmentId = assignment.Id, DriverUser = driver, DriverUserId = driver.Id, ActualStartAt = DateTime.UtcNow.AddDays(-1), ActualEndAt = DateTime.UtcNow.AddDays(-1).AddHours(4) };
        foreach (var passenger in passengers)
            trip.Participants.Add(new FleetTripParticipant { Id = Guid.NewGuid(), Trip = trip, TripId = trip.Id, User = passenger, UserId = passenger.Id, ParticipantType = FleetPassengerTypes.Employee, IsActualParticipant = true, CreatedByUserId = driver.Id });
        for (var index = 0; index < 3; index++)
            trip.Feedbacks.Add(new FleetTripFeedback { Id = Guid.NewGuid(), Trip = trip, TripId = trip.Id, FleetRequest = request, FleetRequestId = request.Id, VehicleAssignment = assignment, VehicleAssignmentId = assignment.Id, Vehicle = vehicle, VehicleId = vehicle.Id, DriverUser = driver, DriverUserId = driver.Id, SubmittedByUser = passengers[index], SubmittedByUserId = passengers[index].Id, PunctualityRating = 5, SafetyRating = 4, ServiceRating = 5, OverallRating = 4, VehicleConditionRating = 4, VehicleCleanlinessRating = 5 });
        db.AddRange(manager, driver); db.AddRange(passengers); db.Add(trip); await db.SaveChangesAsync();

        var controller = Controller(db, manager.Id);
        var action = await controller.Trips(new FleetFeedbackReportQuery { Page = 1, PageSize = 10 }, default);
        var response = Assert.IsType<ApiResponse<FleetFeedbackReportPageDto<FleetFeedbackTripSummaryDto>>>(action.Value);
        var row = Assert.Single(response.Data!.Items);
        Assert.Equal(5, row.EligibleParticipants); Assert.Equal(3, row.FeedbackCount); Assert.Equal(60m, row.ResponseRate);
        Assert.Equal(4d, row.OverallAverage); Assert.Equal(4d, row.SafetyAverage);
        Assert.DoesNotContain(typeof(FleetFeedbackTripSummaryDto).GetProperties(), p => p.Name.Contains("SubmittedBy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ManagementDtos_DoNotExposeReviewerIdentity()
    {
        foreach (var type in new[]
        {
            typeof(FleetFeedbackTripSummaryDto),
            typeof(FleetFeedbackDriverSummaryDto),
            typeof(FleetFeedbackVehicleSummaryDto),
            typeof(FleetFeedbackAttentionDto)
        })
            Assert.DoesNotContain(type.GetProperties(), p => p.Name.Contains("SubmittedBy", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Reviewer", StringComparison.OrdinalIgnoreCase));
    }

    private static User User(string name) => new() { Id = Guid.NewGuid(), Username = name, FullName = name, IsActive = true };
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static FleetFeedbackReportsController Controller(AppDbContext db, Guid actor) => new(
        db,
        Options.Create(new FleetOperationsOptions { FeedbackAttentionThreshold = 2 }))
        { ControllerContext = Context(actor) };
    private static ControllerContext Context(Guid actor) => new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actor.ToString())], "test")) } };
}
