using System.Security.Claims;
using System.Text;
using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public class FleetVehicleUsageTests
{
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static readonly DateTime Start = new(2026, 9, 30, 17, 0, 0, DateTimeKind.Utc);
    private static VehicleUsageQuery Query => new(new(2026, 10, 1), new(2026, 10, 1), null, null);
    private static FleetVehicleUsageController Controller(AppDbContext db) => new(db, new FleetDateRangeService(Options.Create(new FleetOperationsOptions()))) {
        ControllerContext = new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())])) } }
    };
    private static FleetRequest Request(string no, string status = "COMPLETED", DateTime? departure = null) => new() {
        RequestNo = no, Status = status, DepartureAt = departure ?? Start, ExpectedReturnAt = Start.AddHours(4), Destination = "โรงพยาบาลน่าน"
    };
    private static FleetAssignment Assign(FleetRequest request, string code, bool active = true) => new() {
        FleetRequest = request, Vehicle = new() { VehicleCode = code, RegistrationNumber = code }, DriverUser = new() { FullName = "คนขับล่าสุด" }, IsActive = active, AssignedAt = Start
    };

    [Fact]
    public async Task Actual_trips_keep_original_vehicle_and_driver_and_count_requests_once()
    {
        await using var db = Db();
        var request = Request("VH-1"); var old = Assign(request, "OLD", false); var current = Assign(request, "NEW");
        db.FleetAssignments.AddRange(old, current);
        db.FleetTripRecords.AddRange(new() { FleetRequest = request, Assignment = old, DriverUser = new() { FullName = "คนขับจริง" }, ActualStartAt = Start, ActualEndAt = Start.AddMonths(1), StartMileage = 100, EndMileage = 125 },
            new() { FleetRequest = request, Assignment = old, DriverUser = new() { FullName = "คนขับจริง" }, ActualStartAt = Start.AddHours(1), ActualEndAt = Start.AddHours(2), StartMileage = 125, EndMileage = 125 });
        await db.SaveChangesAsync();
        var result = (await Controller(db).Get(Query, default)).Value!.Data!;
        var group = Assert.Single(result.Vehicles); Assert.Equal("OLD", group.VehicleCode); Assert.Equal(1, group.RequestCount); Assert.Equal(2, group.TripCount);
        Assert.Equal(25, group.DistanceKm); Assert.Equal(2, group.CalculatedTripCount); Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, row => Assert.Equal("คนขับจริง", row.Driver));
    }

    [Fact]
    public async Task Includes_all_statuses_unassigned_and_latest_active_assignment_with_departure_boundaries()
    {
        await using var db = Db();
        db.FleetRequests.AddRange(Request("draft", "DRAFT"), Request("cancel", "CANCELLED"), Request("before", departure: Start.AddTicks(-1)), Request("after", departure: Start.AddDays(1)));
        var pending = Request("pending", "READY");
        db.FleetAssignments.AddRange(Assign(pending, "old", false), Assign(pending, "active")); await db.SaveChangesAsync();
        var controller = Controller(db); var result = (await controller.Get(Query, default)).Value!.Data!;
        Assert.Equal(3, result.TotalItems); Assert.Contains(result.Items, x => x.Status == "DRAFT"); Assert.Contains(result.Items, x => x.Status == "CANCELLED");
        Assert.Equal("active", result.Items.Single(x => x.RequestNo == "pending").VehicleCode);
        var unassigned = (await controller.Get(Query with { VehicleId = "unassigned" }, default)).Value!.Data!;
        Assert.Equal(2, unassigned.TotalItems);
        var cancelled = (await controller.Get(Query with { Status = "CANCELLED", PageSize = 1 }, default)).Value!.Data!;
        Assert.Equal("cancel", Assert.Single(cancelled.Items).RequestNo);
    }

    [Fact]
    public async Task Mileage_rules_paging_and_export_match_without_export_page_limit()
    {
        await using var db = Db();
        var assignment = Assign(Request("trip"), "CAR"); db.FleetAssignments.Add(assignment);
        foreach (var (start, end, aborted, ended) in new (decimal?, decimal?, bool, bool)[] { (1, 10, false, true), (0, 0, false, true), (10, 9, false, true), (null, 20, false, true), (0, 30, true, true), (0, null, false, false) })
            db.FleetTripRecords.Add(new() { FleetRequest = assignment.FleetRequest, Assignment = assignment, DriverUser = assignment.DriverUser, ActualStartAt = Start, ActualEndAt = ended ? Start.AddHours(1) : null, StartMileage = start, EndMileage = end, IsAborted = aborted });
        await db.SaveChangesAsync(); var controller = Controller(db);
        var result = (await controller.Get(Query with { PageSize = 2, Page = 2 }, default)).Value!.Data!;
        Assert.Equal(6, result.TotalItems); Assert.Equal(3, result.TotalPages); Assert.Equal(2, result.Items.Count);
        Assert.Equal(9, result.Vehicles[0].DistanceKm); Assert.Equal(2, result.Vehicles[0].CalculatedTripCount);
        var csv = Assert.IsType<FileContentResult>(await controller.Export(Query with { PageSize = 1 }, default));
        var text = Encoding.UTF8.GetString(csv.FileContents); Assert.Equal(7, text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("01/10/2569", text); Assert.Single(db.AuditLogs);
        Assert.IsType<BadRequestObjectResult>((await controller.Get(Query with { VehicleId = "bad" }, default)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.Get(Query with { EndDate = new(2026, 9, 1) }, default)).Result);
    }

    [Theory]
    [InlineData("READY")]
    [InlineData("COMPLETED")]
    public async Task Mileage_history_ignores_filters_and_uses_actual_reading_times(string status)
    {
        await using var db = Db();
        var vehicle = new FleetVehicle { VehicleCode = "CAR", RegistrationNumber = "กข", CurrentMileage = 999999 };
        FleetTripRecord History(string no, DateTime time, decimal? start, decimal? end, bool aborted = false) {
            var request = Request(no, aborted ? "CANCELLED" : "COMPLETED", time);
            return new() { FleetRequest = request, Assignment = new() { FleetRequest = request, Vehicle = vehicle, IsActive = false },
                ActualStartAt = time, ActualEndAt = end != null ? time.AddHours(1) : null, StartMileage = start, EndMileage = end, IsAborted = aborted };
        }
        var visible = Request("visible", status); var assignment = Assign(visible, "unused"); assignment.Vehicle = vehicle;
        db.FleetAssignments.Add(assignment);
        db.FleetTripRecords.AddRange(History("negative", Start.AddMonths(-3), -1, null), History("first", Start.AddMonths(-2), 0, 10),
            History("maximum", Start.AddMonths(-1), 500, 900), History("aborted", Start.AddDays(3), 100, 110, true),
            History("latest-start", Start.AddDays(4), 90, null));
        await db.SaveChangesAsync();
        var result = (await Controller(db).Get(Query with { Status = status }, default)).Value!.Data!;
        var group = Assert.Single(result.Vehicles);
        Assert.Equal(0, group.FirstRecordedMileage); Assert.Equal(Start.AddMonths(-2), group.FirstRecordedMileageAt);
        Assert.Equal(90, group.LatestRecordedMileage); Assert.Equal(Start.AddDays(4), group.LatestRecordedMileageAt);
        Assert.Null(group.DistanceKm);
        var next = History("new-end", Start.AddDays(5), 80, 70, true); db.FleetTripRecords.Add(next); await db.SaveChangesAsync();
        group = Assert.Single((await Controller(db).Get(Query with { Status = status }, default)).Value!.Data!.Vehicles);
        Assert.Equal(70, group.LatestRecordedMileage); Assert.Equal(next.ActualEndAt, group.LatestRecordedMileageAt);
    }

    [Fact]
    public async Task Missing_history_and_unassigned_groups_have_no_mileage()
    {
        await using var db = Db(); db.FleetAssignments.Add(Assign(Request("assigned"), "CAR")); db.FleetRequests.Add(Request("none"));
        await db.SaveChangesAsync();
        var result = (await Controller(db).Get(Query, default)).Value!.Data!;
        Assert.All(result.Vehicles, x => { Assert.Null(x.FirstRecordedMileage); Assert.Null(x.LatestRecordedMileage); });
    }

    [Theory]
    [InlineData("Get", FleetPermissions.ReportView)]
    [InlineData("Export", FleetPermissions.ReportExport)]
    public void Permissions_are_enforced(string method, string permission) => Assert.Equal(permission,
        Assert.Single(typeof(FleetVehicleUsageController).GetMethod(method)!.GetCustomAttributes(typeof(RequirePermissionAttribute), true).Cast<RequirePermissionAttribute>()).PermissionCode);
}
