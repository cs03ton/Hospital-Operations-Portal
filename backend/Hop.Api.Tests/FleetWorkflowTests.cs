using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetWorkflowTests
{
    [Fact]
    public void WorkflowModel_HasConcurrencyAndActiveAssignmentProtection()
    {
        using var db = CreateDb();
        var requestModel = db.Model.FindEntityType(typeof(FleetRequest))!;
        Assert.True(requestModel.FindProperty(nameof(FleetRequest.ConcurrencyToken))!.IsConcurrencyToken);
        var assignmentModel = db.Model.FindEntityType(typeof(FleetAssignment))!;
        var unique = assignmentModel.GetIndexes().Single(x => x.IsUnique && x.Properties.Single().Name == nameof(FleetAssignment.FleetRequestId));
        Assert.Equal("is_active = true", unique.GetFilter());
    }

    [Fact]
    public void SaveRequestValidation_RequiresUtcRangeAndMatchingPassengers()
    {
        var dto = new SaveFleetRequestDto { Purpose = "ประชุม", MissionType = "ทั่วไป", Destination = "จังหวัด", ContactPersonName = "ผู้ประสาน", ContactPhone = "1", DepartureAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Local), ExpectedReturnAt = DateTime.UtcNow.AddHours(-1), PassengerCount = 2, Passengers = [] };
        var results = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), results, true));
        Assert.Contains(results, x => x.ErrorMessage!.Contains("UTC"));
        Assert.Contains(results, x => x.ErrorMessage!.Contains("Passenger count"));
    }

    [Fact]
    public async Task RequestNumber_UsesMonthlyFourDigitFormat()
    {
        await using var db = CreateDb();
        db.FleetRequests.Add(MinimalRequest("VH-202608-0001")); await db.SaveChangesAsync();
        var service = new FleetRequestNumberService(db);
        Assert.Equal("VH-202608-0002", await service.GenerateAsync(new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc), default));
    }

    [Fact]
    public async Task RequestList_IncludesRequestsWhereCurrentUserIsAPassenger()
    {
        await using var db = CreateDb();
        var requester = new User { Id = Guid.NewGuid(), Username = "requester", FullName = "ผู้ขอ" };
        var passenger = new User { Id = Guid.NewGuid(), Username = "passenger", FullName = "ผู้ร่วมเดินทาง" };
        var otherRequester = new User { Id = Guid.NewGuid(), Username = "other", FullName = "ผู้ขออื่น" };
        var sharedRequest = MinimalRequest("VH-202608-0100", requester.Id);
        sharedRequest.Passengers.Add(new FleetRequestPassenger
        {
            Id = Guid.NewGuid(),
            UserId = passenger.Id,
            FullName = passenger.FullName,
            PassengerType = FleetPassengerTypes.Employee,
            SortOrder = 1
        });
        var unrelatedRequest = MinimalRequest("VH-202608-0101", otherRequester.Id);
        db.AddRange(requester, passenger, otherRequester, sharedRequest, unrelatedRequest);
        await db.SaveChangesAsync();

        var controller = new FleetRequestsController(
            db,
            new FleetRequestNumberService(db),
            new FleetAvailabilityService(db));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, passenger.Id.ToString())],
                    "Test"))
            }
        };

        var result = await controller.GetMinePaged(ct: default);

        var response = Assert.IsType<ApiResponse<PagedResponse<FleetRequestDto>>>(result.Value);
        var item = Assert.Single(response.Data!.Items);
        Assert.Equal(sharedRequest.Id, item.Id);
    }

    [Fact]
    public async Task Availability_RejectsOverlapUnavailabilityLeaveAndExpiredLicense()
    {
        await using var db = CreateDb();
        var requester = new User { Id = Guid.NewGuid(), Username = "requester", FullName = "Requester" };
        var driver = new User { Id = Guid.NewGuid(), Username = "driver", FullName = "Driver" };
        var leaveType = new LeaveType { Id = Guid.NewGuid(), Code = "SICK", Name = "ลาป่วย" };
        var type = new FleetVehicleType { Id = Guid.NewGuid(), Code = "SEDAN", Name = "รถเก๋ง" };
        var vehicle = new FleetVehicle { Id = Guid.NewGuid(), VehicleCode = "V1", RegistrationNumber = "TEST", VehicleTypeId = type.Id, PassengerCapacity = 4, SeatCapacityTotal = 5 };
        var request = MinimalRequest("VH-202608-0001", requester.Id); request.RequestedVehicleTypeId = type.Id;
        db.AddRange(requester, driver, leaveType, type, vehicle,
            new FleetVehicleUnavailability { Id = Guid.NewGuid(), VehicleId = vehicle.Id, StartAt = request.DepartureAt, EndAt = request.ExpectedReturnAt, Reason = "ซ่อม", CreatedByUserId = requester.Id },
            new FleetDriverProfile { Id = Guid.NewGuid(), UserId = driver.Id, LicenseNumber = "X", LicenseType = "CAR", LicenseExpiryDate = new DateOnly(2026, 7, 31), CanDriveSedan = true },
            new LeaveRequest { Id = Guid.NewGuid(), UserId = driver.Id, LeaveTypeId = leaveType.Id, StartDate = new DateOnly(2026, 8, 1), EndDate = new DateOnly(2026, 8, 1), TotalDays = 1, Reason = "ลา", Status = "Approved" });
        await db.SaveChangesAsync();
        var result = await new FleetAvailabilityService(db).GetAsync(request, default);
        Assert.False(result.Vehicles.Single().IsAvailable);
        Assert.Contains("รถมีช่วงงดใช้งานชนเวลา", result.Vehicles.Single().Reasons);
        Assert.False(result.Drivers.Single().IsAvailable);
        Assert.Contains("คนขับมีใบลาอนุมัติชนช่วงเดินทาง", result.Drivers.Single().Reasons);
        Assert.Contains("ใบขับขี่หมดอายุก่อนจบภารกิจ", result.Drivers.Single().Reasons);
    }

    private static FleetRequest MinimalRequest(string no, Guid? userId = null) => new() { Id = Guid.NewGuid(), RequestNo = no, RequesterUserId = userId ?? Guid.NewGuid(), CreatedByUserId = userId ?? Guid.NewGuid(), Purpose = "Test", MissionType = "GENERAL", Destination = "Test", ContactPersonName = "Test", ContactPhone = "1", DepartureAt = new DateTime(2026, 8, 1, 1, 0, 0, DateTimeKind.Utc), ExpectedReturnAt = new DateTime(2026, 8, 1, 5, 0, 0, DateTimeKind.Utc), PassengerCount = 1 };
    private static AppDbContext CreateDb() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
}
