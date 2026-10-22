using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetCalendarTripTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Calendar_previews_include_requester_destination_vehicle_and_driver_without_patient_details(bool activeAssignment)
    {
        await using var db = Database();
        var trip = await Seed(db, Utc(4, 2), Utc(4, 6));
        trip.Assignment!.IsActive = activeAssignment;
        trip.FleetRequest!.PatientName = "Private Patient";
        trip.FleetRequest.ContactPhone = "0812345678";
        await db.SaveChangesAsync();
        var controller = new FleetCalendarController(db, new FleetDateRangeService(
            Options.Create(new FleetOperationsOptions { MaximumCalendarRangeDays = 40 })));
        var response = await controller.Get(new DateOnly(2026, 10, 4), new DateOnly(2026, 10, 4),
            "REQUEST,ASSIGNMENT,TRIP", null, null, null, null);
        var entries = Assert.IsAssignableFrom<IEnumerable<CalendarEventDto>>(
            Assert.IsType<ApiResponse<object>>(response.Value).Data).ToList();
        Assert.Equal(3, entries.Count);
        foreach (var entry in entries)
        {
            using var metadata = JsonDocument.Parse(JsonSerializer.Serialize(entry.Metadata));
            Assert.Equal("QA Requester", metadata.RootElement.GetProperty("Requester").GetString());
            Assert.Equal("QA Department", metadata.RootElement.GetProperty("Department").GetString());
            Assert.Equal("Nan Hospital", metadata.RootElement.GetProperty("Destination").GetString());
            Assert.Equal("Coordination meeting", metadata.RootElement.GetProperty("Purpose").GetString());
            Assert.Equal("QA", metadata.RootElement.GetProperty("Vehicle").GetString());
            Assert.Equal("QA-1234", metadata.RootElement.GetProperty("RegistrationNumber").GetString());
            Assert.Equal("QA Driver", metadata.RootElement.GetProperty("Driver").GetString());
            Assert.DoesNotContain("Private Patient", metadata.RootElement.GetRawText());
            Assert.DoesNotContain("0812345678", metadata.RootElement.GetRawText());
        }
    }

    [Theory]
    [InlineData("IN_PROGRESS")]
    [InlineData("COMPLETED")]
    [InlineData("ABORTED")]
    public async Task Trip_uses_requested_schedule_even_when_actual_end_is_missing_or_much_later(string status)
    {
        await using var db = Database();
        var trip = await Seed(db, Utc(4, 2), Utc(4, 6)); // 09:00–13:00 Bangkok.
        trip.ActualStartAt = Utc(4, 2).AddMinutes(14);
        trip.ActualEndAt = status == "COMPLETED" ? Utc(22, 7) : null;
        trip.IsAborted = status == "ABORTED";
        trip.AbortedAt = trip.IsAborted ? Utc(22, 7) : null;
        await db.SaveChangesAsync();

        var entry = Assert.Single(await Events(db, 4, 4));
        Assert.Equal(Utc(4, 2), entry.StartAt);
        Assert.Equal(Utc(4, 6), entry.EndAt);
        Assert.Equal(status, entry.Status);
        using var metadata = JsonDocument.Parse(JsonSerializer.Serialize(entry.Metadata));
        Assert.Equal(trip.ActualStartAt, metadata.RootElement.GetProperty("ActualStartAt").GetDateTime());
        Assert.Empty(await Events(db, 5, 5));
        Assert.Empty(await Events(db, 18, 24));
        Assert.Equal(trip.ActualEndAt, (await db.FleetTripRecords.SingleAsync()).ActualEndAt);
    }

    [Fact]
    public async Task Multi_day_schedule_includes_each_travel_day_and_respects_Bangkok_midnight()
    {
        await using var db = Database();
        // 5 Oct 00:00 through 7 Oct 23:59 Bangkok.
        await Seed(db, Utc(4, 17), Utc(7, 16).AddMinutes(59));
        Assert.Empty(await Events(db, 4, 4));
        foreach (var day in new[] { 5, 6, 7 }) Assert.Single(await Events(db, day, day));
        Assert.Empty(await Events(db, 8, 8));
    }

    [Fact]
    public async Task Trip_filters_match_its_assigned_vehicle_driver_and_request_department()
    {
        await using var db = Database();
        var trip = await Seed(db, Utc(4, 2), Utc(4, 6));
        Assert.Single(await Events(db, 4, 4, trip.Assignment!.VehicleId, trip.DriverUserId,
            trip.FleetRequest!.RequesterDepartmentId));
        Assert.Empty(await Events(db, 4, 4, vehicle: Guid.NewGuid()));
        Assert.Empty(await Events(db, 4, 4, driver: Guid.NewGuid()));
        Assert.Empty(await Events(db, 4, 4, department: Guid.NewGuid()));
    }

    private static async Task<List<CalendarEventDto>> Events(AppDbContext db, int start, int end,
        Guid? vehicle = null, Guid? driver = null, Guid? department = null)
    {
        var controller = new FleetCalendarController(db, new FleetDateRangeService(
            Options.Create(new FleetOperationsOptions { MaximumCalendarRangeDays = 40 })));
        var response = await controller.Get(new DateOnly(2026, 10, start), new DateOnly(2026, 10, end),
            "TRIP", vehicle, driver, department, null, false);
        return Assert.IsAssignableFrom<IEnumerable<CalendarEventDto>>(
            Assert.IsType<ApiResponse<object>>(response.Value).Data).ToList();
    }

    private static async Task<FleetTripRecord> Seed(AppDbContext db, DateTime departure, DateTime expectedReturn)
    {
        var request = new FleetRequest { RequestNo = "VH-202610-0009", DepartureAt = departure,
            ExpectedReturnAt = expectedReturn, Status = FleetRequestStatuses.InProgress,
            RequesterDepartment = new Department { Name = "QA Department" },
            RequesterUser = new User { Username = "qa-requester", FullName = "QA Requester" },
            Destination = "Nan Hospital", Purpose = "Coordination meeting", PassengerCount = 3 };
        var assignment = new FleetAssignment { FleetRequest = request, Vehicle = new FleetVehicle
            { VehicleCode = "QA", RegistrationNumber = "QA-1234" }, DriverUser = new User { Username = "qa-driver", FullName = "QA Driver" } };
        var trip = new FleetTripRecord { FleetRequest = request, Assignment = assignment,
            DriverUser = assignment.DriverUser, ActualStartAt = departure, StartMileage = 89286 };
        db.Add(trip);
        await db.SaveChangesAsync();
        return trip;
    }

    private static DateTime Utc(int day, int hour) => new(2026, 10, day, hour, 0, 0, DateTimeKind.Utc);
    private static AppDbContext Database() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase($"calendar-trip-{Guid.NewGuid()}").Options);
}
