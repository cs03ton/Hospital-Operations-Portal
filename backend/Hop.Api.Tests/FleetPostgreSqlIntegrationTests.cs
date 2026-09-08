using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.DTOs;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace Hop.Api.Tests;

[CollectionDefinition("FleetPostgreSql", DisableParallelization = true)] public sealed class FleetPostgreSqlCollection;
[Collection("FleetPostgreSql")]
public sealed class FleetPostgreSqlIntegrationTests
{
    [Fact, Trait("Category", "PostgreSqlIntegration")]
    public async Task Utilization_uses_PostgreSQL_multirange_and_expected_overlap()
    {
        await using var db = await Database(); if (db is null) return; var data = await Seed(db); var service = new FleetUtilizationQueryService(db); var rows = await service.Query(data.Start, data.End, data.VehicleA.Id, data.Start.AddHours(20), default); var a = Assert.Single(rows);
        Assert.Equal(24, a.ReportingWindowHours); Assert.Equal(6, a.AssignedHours); Assert.Equal(4, a.TripHours); Assert.Equal(4, a.UnavailableHours); Assert.Equal(4, a.MaintenanceHours); Assert.Equal(6, a.BlockedHours); Assert.Equal(18, a.AvailableHours); Assert.InRange(a.UtilizationPercentage!.Value, 22.2221m, 22.2223m);
        var noData = Assert.Single(await service.Query(data.Start, data.End, data.VehicleC.Id, data.Start.AddHours(20), default)); Assert.Equal(0, noData.AssignedHours); Assert.Equal(24, noData.AvailableHours); Assert.Equal(0, noData.UtilizationPercentage);
        await using var command = new NpgsqlCommand("select current_setting('server_version_num')::int >= 140000, '{}'::tstzmultirange is not null", (NpgsqlConnection)db.Database.GetDbConnection()); await using var reader = await command.ExecuteReaderAsync(); Assert.True(await reader.ReadAsync() && reader.GetBoolean(0) && reader.GetBoolean(1));
    }

    [Fact, Trait("Category", "PostgreSqlIntegration")]
    public async Task Calendar_projects_every_source_with_stable_unique_ids_and_filters()
    {
        await using var db = await Database(); if (db is null) return; var data = await Seed(db); var ranges = new FleetDateRangeService(Options.Create(new FleetOperationsOptions { MaximumCalendarRangeDays = 40 })); var controller = new FleetCalendarController(db, ranges); var result = await controller.Get(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), null, null, null, null, null, true, default); var payload = Assert.IsType<ApiResponse<object>>(result.Value); var events = Assert.IsAssignableFrom<IEnumerable<CalendarEventDto>>(payload.Data).ToList(); var expected = new[] { "REQUEST", "ASSIGNMENT", "TRIP", "VEHICLE_UNAVAILABILITY", "DRIVER_UNAVAILABILITY", "MAINTENANCE", "VEHICLE_DOCUMENT_EXPIRY", "CANCELLATION_REQUEST" }; foreach (var type in expected) Assert.Contains(events, x => x.EventType == type); Assert.Equal(events.Count, events.Select(x => x.Id).Distinct().Count()); Assert.All(events, x => Assert.StartsWith(x.EventType switch { "REQUEST" => "request:", "ASSIGNMENT" => "assignment:", "TRIP" => "trip:", "VEHICLE_UNAVAILABILITY" => "vehicle-unavailability:", "DRIVER_UNAVAILABILITY" => "driver-unavailability:", "MAINTENANCE" => "maintenance:", "VEHICLE_DOCUMENT_EXPIRY" => "document:", _ => "cancellation:" }, x.Id)); Assert.DoesNotContain(events.Select(x => System.Text.Json.JsonSerializer.Serialize(x.Metadata)), x => x.Contains("license", StringComparison.OrdinalIgnoreCase) || x.Contains("documentNumber", StringComparison.OrdinalIgnoreCase));
        var filtered = await controller.Get(new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 2), "TRIP", data.VehicleA.Id, data.Driver.Id, data.Department.Id, null, true, default); var filteredEvents = Assert.IsAssignableFrom<IEnumerable<CalendarEventDto>>(Assert.IsType<ApiResponse<object>>(filtered.Value).Data); Assert.All(filteredEvents, x => Assert.Equal("TRIP", x.EventType));
    }

    private static async Task<AppDbContext?> Database()
    {
        var connection = Environment.GetEnvironmentVariable("HOP_E2E_CONNECTION_STRING"); if (string.IsNullOrWhiteSpace(connection)) { if (Environment.GetEnvironmentVariable("FLEET_REQUIRE_POSTGRES_INTEGRATION") == "true") throw new InvalidOperationException("HOP_E2E_CONNECTION_STRING is required for PostgreSqlIntegration tests."); return null; }
        var cs = new NpgsqlConnectionStringBuilder(connection); if (!new[] { "qa", "test", "ci", "uat" }.Any(x => cs.Database.Contains(x, StringComparison.OrdinalIgnoreCase))) throw new InvalidOperationException("Refusing non-test PostgreSQL database."); var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options); await db.Database.EnsureDeletedAsync(); await db.Database.MigrateAsync(); return db;
    }
    private static async Task<Data> Seed(AppDbContext db)
    {
        var s = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc); var department = new Department { Name = "qa-dept" }; var user = new User { EmployeeCode = "qa-requester", Username = "qa-requester", FullName = "QA Requester", PasswordHash = "x", Department = department }; var driverUser = new User { EmployeeCode = "qa-driver", Username = "qa-driver", FullName = "QA Driver", PasswordHash = "x", Department = department }; var admin = new User { EmployeeCode = "qa-admin", Username = "qa-admin", FullName = "QA Admin", PasswordHash = "x", Department = department }; var type = new FleetVehicleType { Code = "QA", Name = "QA" }; var a = new FleetVehicle { VehicleCode = "QA-A", RegistrationNumber = "QA-A", VehicleType = type, SeatCapacityTotal = 4, PassengerCapacity = 3, CurrentMileage = 1000 }; var c = new FleetVehicle { VehicleCode = "QA-C", RegistrationNumber = "QA-C", VehicleType = type, SeatCapacityTotal = 4, PassengerCapacity = 3, CurrentMileage = 0 }; db.AddRange(department, user, driverUser, admin, type, a, c);
        var r1 = Request("VH-QA-1", s.AddHours(8), s.AddHours(12)); var r2 = Request("VH-QA-2", s.AddHours(10), s.AddHours(14)); r1.RequesterUser = user; r1.RequesterDepartment = department; r1.CreatedByUserId = user.Id; r2.RequesterUser = user; r2.RequesterDepartment = department; r2.CreatedByUserId = user.Id; var as1 = Assignment(r1, a, driverUser, admin, s.AddHours(7)); var as2 = Assignment(r2, a, driverUser, admin, s.AddHours(9)); db.AddRange(r1, r2, as1, as2); db.FleetTripRecords.AddRange(new FleetTripRecord { FleetRequest = r1, Assignment = as1, DriverUser = driverUser, ActualStartAt = s.AddHours(9), ActualEndAt = s.AddHours(11), StartMileage = 1000, EndMileage = 1020 }, new FleetTripRecord { FleetRequest = r2, Assignment = as2, DriverUser = driverUser, ActualStartAt = s.AddHours(10.5), ActualEndAt = s.AddHours(13), StartMileage = 1020, EndMileage = 1040 }); db.FleetVehicleUnavailability.Add(new FleetVehicleUnavailability { Vehicle = a, StartAt = s.AddHours(14), EndAt = s.AddHours(18), Reason = "QA", Type = "OTHER", CreatedByUser = admin }); db.FleetDriverUnavailability.Add(new FleetDriverUnavailability { User = driverUser, StartAt = s.AddHours(20), EndAt = s.AddHours(22), Reason = "QA", CreatedByUser = admin }); var mt = new FleetMaintenanceType { Code = "QA-M", Name = "QA maintenance", Category = "Inspection", IsDateBased = true, IsMileageBased = false, BlocksAvailabilityWhenOverdue = true, CreatedByUserId = admin.Id }; var ms = new FleetVehicleMaintenanceSchedule { Vehicle = a, MaintenanceType = mt, Status = FleetMaintenanceStatuses.InProgress, DueDate = s.AddHours(16), CreatedAt = s.AddHours(16), UpdatedAt = s.AddHours(16), CreatedByUserId = admin.Id }; db.AddRange(mt, ms); db.FleetVehicleMaintenanceRecords.Add(new FleetVehicleMaintenanceRecord { VehicleId = a.Id, Schedule = ms, MaintenanceTypeId = mt.Id, StartedAt = s.AddHours(16), StartMileage = 1040, CreatedByUserId = admin.Id }); db.FleetVehicleDocuments.Add(new FleetVehicleDocument { Vehicle = a, DocumentType = "INSURANCE", DocumentNumber = "SECRET", ExpiresAt = s.AddHours(23), CreatedByUserId = admin.Id }); db.FleetCancellationRequests.Add(new FleetCancellationRequest { FleetRequest = r1, RequestedByUser = user, PreviousStatus = FleetRequestStatuses.Ready, Reason = "QA", Status = "APPROVED", CreatedAt = s.AddHours(6), CompletedAt = s.AddHours(7), ReviewedByUser = admin }); await db.SaveChangesAsync(); return new(s, s.AddDays(1), a, c, driverUser, department);
        FleetRequest Request(string no, DateTime start, DateTime end) => new() { RequestNo = no, RequestDate = new DateOnly(2026, 8, 1), Purpose = "QA", MissionType = "QA", Destination = "QA", ContactPersonName = "QA", ContactPhone = "0", DepartureAt = start, ExpectedReturnAt = end, PassengerCount = 1, Status = FleetRequestStatuses.Completed, CreatedAt = s }; FleetAssignment Assignment(FleetRequest r, FleetVehicle v, User d, User by, DateTime at) => new() { FleetRequest = r, Vehicle = v, DriverUser = d, AssignedByUser = by, AssignedAt = at, IsActive = false, AssignmentStatus = FleetAssignmentStatuses.Completed };
    }
    private sealed record Data(DateTime Start, DateTime End, FleetVehicle VehicleA, FleetVehicle VehicleC, User Driver, Department Department);
}
