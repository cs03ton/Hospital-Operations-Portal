using System.Text.Json;
using Hop.Api.Configuration;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetReportCancellationTests
{
    [Fact]
    public async Task Report_excludes_cancelled_requests_but_dashboard_default_is_unchanged()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.AddRange(new FleetRequest { Destination = "ปลายทาง", Status = FleetRequestStatuses.Completed },
            new FleetRequest { Destination = "ปลายทาง", Status = FleetRequestStatuses.Cancelled });
        await db.SaveChangesAsync();
        var service = new FleetKpiService(db, new FleetDateRangeService(Options.Create(new FleetOperationsOptions())), new FleetUtilizationQueryService(db), new FleetWorkflowDurationQueryService(db));
        var filter = new FleetReportFilter(null, null, null, null, null, null, null, true);
        using var report = JsonDocument.Parse(JsonSerializer.Serialize(await service.Routes(filter, default)));
        Assert.Equal(1, report.RootElement[0].GetProperty("RequestCount").GetInt32());
        using var dashboard = JsonDocument.Parse(JsonSerializer.Serialize(await service.Routes(filter with { ExcludeCancelled = false }, default)));
        Assert.Equal(2, dashboard.RootElement[0].GetProperty("RequestCount").GetInt32());
        var export = new FleetReportExportService(service, Options.Create(new FleetOperationsOptions()));
        var csv = System.Text.Encoding.UTF8.GetString(await export.Export("routes", filter, default));
        Assert.Contains("\"1\",\"1\"", csv);
    }
}
