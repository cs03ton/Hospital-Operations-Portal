using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Hop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetDestinationOptionsTests
{
    [Fact]
    public async Task Suggestions_are_distinct_ranked_searchable_and_exclude_private_drafts()
    {
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        foreach (var destination in new[] { "โรงพยาบาลน่าน", " โรงพยาบาลน่าน ", "สสจ.น่าน", "  " })
            db.Add(new FleetRequest { Destination = destination, SubmittedAt = DateTime.UtcNow });
        db.Add(new FleetRequest { Destination = "ปลายทางร่าง" });
        await db.SaveChangesAsync();
        var controller = new FleetRequestsController(db, new FleetRequestNumberService(db), new FleetAvailabilityService(db), new DomainEventPublisher(db, new FleetNotificationRecipientResolver(db)));
        var result = await controller.GetDestinationOptions();
        Assert.Equal(new[] { "โรงพยาบาลน่าน", "สสจ.น่าน" }, result.Value!.Data);
        var search = await controller.GetDestinationOptions(" สสจ ");
        Assert.Equal("สสจ.น่าน", Assert.Single(search.Value!.Data!));
        Assert.Empty((await controller.GetDestinationOptions("ไม่มีปลายทางนี้")).Value!.Data!);
    }
}
