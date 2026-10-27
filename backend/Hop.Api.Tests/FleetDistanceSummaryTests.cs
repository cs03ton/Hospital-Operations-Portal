using Hop.Api.Authorization;
using Hop.Api.Configuration;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public class FleetDistanceSummaryTests
{
    private static AppDbContext Db() => new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static FleetUsageReportsController Controller(AppDbContext db) => new(db, new FleetDateRangeService(Options.Create(new FleetOperationsOptions())));
    private static FleetTripRecord Trip(decimal? start, decimal? end, DateTime departure, string status = FleetRequestStatuses.Completed,
        bool aborted = false, bool ended = true) => new()
        {
            StartMileage = start, EndMileage = end, ActualEndAt = ended ? departure.AddMonths(1) : null, IsAborted = aborted,
            FleetRequest = new() { DepartureAt = departure, Status = status, CreatedAt = departure.AddMonths(-2), SubmittedAt = departure.AddMonths(-1) }
        };
    private static readonly DateTime Start = new(2026, 9, 30, 17, 0, 0, DateTimeKind.Utc); // 1 Oct Bangkok

    [Fact]
    public async Task Totals_real_mileage_once_by_departure_with_Thai_day_boundaries()
    {
        await using var db = Db();
        var first = Trip(100, 125.5m, Start);
        first.FleetRequest!.Assignments = [new(), new()];
        db.FleetTripRecords.AddRange(first, Trip(200, 210, Start.AddDays(1).AddTicks(-1)), Trip(0, 999, Start.AddTicks(-1)),
            Trip(0, 999, Start.AddDays(1)), Trip(0, 900, Start, FleetRequestStatuses.Cancelled), Trip(0, 900, Start, aborted: true),
            Trip(0, 900, Start, FleetRequestStatuses.InProgress, ended: false));
        await db.SaveChangesAsync();
        var response = (await Controller(db).DistanceSummary(new(new(2026, 10, 1), new(2026, 10, 1), null), default)).Value!.Data!;
        Assert.Equal(35.5m, response.TotalDistanceKm); Assert.Equal(2, response.CalculatedTripCount); Assert.Equal(0, response.IncompleteTripCount);
    }

    [Fact]
    public async Task Missing_invalid_and_zero_distances_are_distinguished_and_status_is_applied()
    {
        await using var db = Db();
        db.FleetTripRecords.AddRange(Trip(null, 10, Start), Trip(10, null, Start), Trip(10, 9, Start), Trip(-1, 10, Start), Trip(20, 20, Start));
        await db.SaveChangesAsync();
        var controller = Controller(db);
        var query = new FleetDistanceSummaryQuery(new(2026, 10, 1), new(2026, 10, 1), FleetRequestStatuses.Completed);
        var result = (await controller.DistanceSummary(query, default)).Value!.Data!;
        Assert.Equal(0m, result.TotalDistanceKm); Assert.Equal(1, result.CalculatedTripCount); Assert.Equal(4, result.IncompleteTripCount);
        var empty = (await controller.DistanceSummary(query with { Status = FleetRequestStatuses.Ready }, default)).Value!.Data!;
        Assert.Null(empty.TotalDistanceKm); Assert.Equal(0, empty.CalculatedTripCount);
        db.FleetTripRecords.Remove(await db.FleetTripRecords.SingleAsync(x => x.StartMileage == 20)); await db.SaveChangesAsync();
        Assert.Null((await controller.DistanceSummary(query, default)).Value!.Data!.TotalDistanceKm);
    }

    [Fact]
    public async Task Invalid_range_is_rejected_and_report_permission_is_required()
    {
        await using var db = Db();
        Assert.IsType<BadRequestObjectResult>((await Controller(db).DistanceSummary(new(new(2026, 10, 2), new(2026, 10, 1), null), default)).Result);
        var permission = Assert.Single(typeof(FleetUsageReportsController).GetMethod(nameof(FleetUsageReportsController.DistanceSummary))!
            .GetCustomAttributes(typeof(RequirePermissionAttribute), true));
        Assert.Equal(FleetPermissions.ReportView, Assert.IsType<RequirePermissionAttribute>(permission).PermissionCode);
    }
}
