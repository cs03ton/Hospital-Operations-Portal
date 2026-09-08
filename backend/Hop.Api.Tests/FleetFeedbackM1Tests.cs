using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Xunit;
using System.ComponentModel.DataAnnotations;
using Hop.Api.DTOs;

namespace Hop.Api.Tests;

public sealed class FleetFeedbackM1Tests
{
    [Fact]
    public void ParticipantModel_HasEmployeeTripUniquenessAndActualParticipantIndex()
    {
        using var db = CreateDb();
        var model = db.Model.FindEntityType(typeof(FleetTripParticipant))!;
        Assert.Contains(model.GetIndexes(), index =>
            index.IsUnique &&
            index.Properties.Select(x => x.Name).SequenceEqual([nameof(FleetTripParticipant.TripId), nameof(FleetTripParticipant.UserId)]));
        Assert.Contains(model.GetIndexes(), index =>
            index.Properties.Select(x => x.Name).SequenceEqual([nameof(FleetTripParticipant.UserId), nameof(FleetTripParticipant.IsActualParticipant)]));
    }

    [Fact]
    public async Task ActualEmployeeParticipant_CanGiveFeedbackDuringConfiguredWindow()
    {
        await using var db = CreateDb();
        var graph = AddCompletedTrip(db, requesterIsParticipant: true);
        await db.SaveChangesAsync();
        var service = Service(db, windowDays: 7);

        var result = await service.EvaluateAsync(graph.Trip.Id, graph.Requester.Id, graph.CompletedAt.AddDays(7));

        Assert.True(result.CanSubmit);
        Assert.Equal(FleetFeedbackStatuses.Available, result.Status);
        Assert.Equal(graph.CompletedAt.AddDays(7), result.FeedbackDeadline);
    }

    [Fact]
    public async Task RequesterWhoDidNotTravel_AndAssignedDriver_AreNotEligible()
    {
        await using var db = CreateDb();
        var graph = AddCompletedTrip(db, requesterIsParticipant: false);
        await db.SaveChangesAsync();
        var service = Service(db);

        var requester = await service.EvaluateAsync(graph.Trip.Id, graph.Requester.Id, graph.CompletedAt.AddHours(1));
        var driver = await service.EvaluateAsync(graph.Trip.Id, graph.Driver.Id, graph.CompletedAt.AddHours(1));

        Assert.Equal("NOT_ACTUAL_PARTICIPANT", requester.Reason);
        Assert.Equal("DRIVER_CANNOT_REVIEW_OWN_TRIP", driver.Reason);
        Assert.False(requester.CanSubmit);
        Assert.False(driver.CanSubmit);
    }

    [Fact]
    public async Task FeedbackWindow_UsesConfiguredDaysAndExpiresAfterDeadline()
    {
        await using var db = CreateDb();
        var graph = AddCompletedTrip(db, requesterIsParticipant: true);
        await db.SaveChangesAsync();
        var service = Service(db, windowDays: 3);

        var result = await service.EvaluateAsync(graph.Trip.Id, graph.Requester.Id, graph.CompletedAt.AddDays(3).AddTicks(1));

        Assert.Equal(FleetFeedbackStatuses.Expired, result.Status);
        Assert.False(result.CanSubmit);
        Assert.Equal(graph.CompletedAt.AddDays(3), result.FeedbackDeadline);
    }

    [Fact]
    public async Task ParticipantFinalization_AllowsReplacementEmployeeAndDerivesRequesterFlag()
    {
        await using var db = CreateDb();
        var graph = AddInProgressTrip(db);
        var replacement = new User { Id = Guid.NewGuid(), Username = "replacement", FullName = "Replacement", IsActive = true };
        db.Users.Add(replacement);
        await db.SaveChangesAsync();
        var service = new FleetTripParticipantService(db);

        var result = await service.FinalizeEmployeesAsync(graph.Trip.Id, graph.Driver.Id, [graph.Requester.Id, replacement.Id]);

        Assert.Equal(2, result.Count);
        Assert.True(result.Single(x => x.UserId == graph.Requester.Id).IsRequester);
        Assert.False(result.Single(x => x.UserId == replacement.Id).IsRequester);
    }

    [Fact]
    public void FeedbackModel_HasDatabaseDuplicateProtectionAndRatingConstraints()
    {
        using var db = CreateDb();
        var model = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(FleetTripFeedback))!;
        Assert.Contains(model.GetIndexes(), index => index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual([nameof(FleetTripFeedback.TripId), nameof(FleetTripFeedback.SubmittedByUserId)]));
        Assert.True(model.FindProperty(nameof(FleetTripFeedback.ConcurrencyToken))!.IsConcurrencyToken);
        Assert.Contains(model.GetCheckConstraints(), constraint => constraint.Name == "ck_fleet_trip_feedback_ratings");
    }

    [Fact]
    public async Task ExistingFeedback_DerivesSubmittedStatusAndPreventsAnotherSubmission()
    {
        await using var db = CreateDb();
        var graph = AddCompletedTrip(db, requesterIsParticipant: true);
        var assignment = graph.Trip.Assignment!;
        db.FleetTripFeedbacks.Add(new FleetTripFeedback { Id = Guid.NewGuid(), TripId = graph.Trip.Id, FleetRequestId = graph.Trip.FleetRequestId, VehicleAssignmentId = assignment.Id, VehicleId = assignment.VehicleId, DriverUserId = graph.Driver.Id, SubmittedByUserId = graph.Requester.Id, PunctualityRating = 5, SafetyRating = 5, ServiceRating = 5, OverallRating = 5, VehicleConditionRating = 5, VehicleCleanlinessRating = 5 });
        await db.SaveChangesAsync();

        var result = await Service(db).EvaluateAsync(graph.Trip.Id, graph.Requester.Id, graph.CompletedAt.AddHours(1));

        Assert.Equal(FleetFeedbackStatuses.Submitted, result.Status);
        Assert.False(result.CanSubmit);
    }

    [Fact]
    public void IncidentValidation_RequiresCategoryAndComment_AndAllRatings()
    {
        var request = new SubmitFleetTripFeedbackRequest { HasIncident = true, PunctualityRating = 5, SafetyRating = 5, ServiceRating = 5, OverallRating = 5, VehicleConditionRating = 5, VehicleCleanlinessRating = 5 };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), results, true));
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.IncidentCategory)));
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.Comment)));

        request.HasIncident = false;
        request.PunctualityRating = 0;
        results.Clear();
        Assert.False(Validator.TryValidateObject(request, new ValidationContext(request), results, true));
        Assert.Contains(results, x => x.MemberNames.Contains(nameof(request.PunctualityRating)));
    }

    [Fact]
    public void ParticipantSafeContext_DoesNotExposeIdentityOrInternalWorkflowFields()
    {
        var fields = typeof(FleetFeedbackContextDto).GetProperties().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("SubmittedByUserId", fields);
        Assert.DoesNotContain("ApprovalComments", fields);
        Assert.DoesNotContain("AuditHistory", fields);
        Assert.DoesNotContain("PassengerList", fields);
        Assert.Contains("FeedbackStatus", fields);
    }

    private static FleetFeedbackEligibilityService Service(AppDbContext db, int windowDays = 7) =>
        new(db, Options.Create(new FleetOperationsOptions { FeedbackWindowDays = windowDays }));

    private static (FleetTripRecord Trip, User Requester, User Driver, DateTime CompletedAt) AddCompletedTrip(AppDbContext db, bool requesterIsParticipant)
    {
        var graph = AddInProgressTrip(db);
        var completedAt = new DateTime(2026, 8, 18, 5, 0, 0, DateTimeKind.Utc);
        graph.Request.Status = FleetRequestStatuses.Completed;
        graph.Trip.ActualEndAt = completedAt;
        if (requesterIsParticipant)
            graph.Trip.Participants.Add(new FleetTripParticipant { Id = Guid.NewGuid(), UserId = graph.Requester.Id, IsRequester = true, ParticipantType = FleetPassengerTypes.Employee, IsActualParticipant = true, CreatedByUserId = graph.Driver.Id });
        return (graph.Trip, graph.Requester, graph.Driver, completedAt);
    }

    private static (FleetTripRecord Trip, FleetRequest Request, User Requester, User Driver) AddInProgressTrip(AppDbContext db)
    {
        var requester = new User { Id = Guid.NewGuid(), Username = "requester", FullName = "Requester", IsActive = true };
        var driver = new User { Id = Guid.NewGuid(), Username = "driver", FullName = "Driver", IsActive = true };
        var vehicleType = new FleetVehicleType { Id = Guid.NewGuid(), Code = Guid.NewGuid().ToString("N"), Name = "Van" };
        var vehicle = new FleetVehicle { Id = Guid.NewGuid(), VehicleCode = Guid.NewGuid().ToString("N"), RegistrationNumber = Guid.NewGuid().ToString("N"), VehicleTypeId = vehicleType.Id, SeatCapacityTotal = 4, PassengerCapacity = 4, CreatedByUserId = requester.Id };
        var request = new FleetRequest { Id = Guid.NewGuid(), RequestNo = $"VH-{Guid.NewGuid():N}", RequesterUserId = requester.Id, CreatedByUserId = requester.Id, Purpose = "Test", MissionType = "GENERAL", Destination = "Test", ContactPersonName = "Test", ContactPhone = "1", DepartureAt = DateTime.UtcNow, ExpectedReturnAt = DateTime.UtcNow.AddHours(2), PassengerCount = 1, Status = FleetRequestStatuses.InProgress };
        var assignment = new FleetAssignment { Id = Guid.NewGuid(), FleetRequestId = request.Id, VehicleId = vehicle.Id, DriverUserId = driver.Id, AssignedByUserId = requester.Id };
        var trip = new FleetTripRecord { Id = Guid.NewGuid(), FleetRequestId = request.Id, AssignmentId = assignment.Id, DriverUserId = driver.Id, ActualStartAt = DateTime.UtcNow, FleetRequest = request, Assignment = assignment };
        db.AddRange(requester, driver, vehicleType, vehicle, request, assignment, trip);
        return (trip, request, requester, driver);
    }

    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
