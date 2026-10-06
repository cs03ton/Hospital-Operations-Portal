using System.Reflection;
using Hop.Api.Controllers;
using Hop.Api.Data;
using Hop.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Hop.Api.Tests;

public sealed class FleetRequestSaveErrorTests
{
    [Fact]
    public void NewStatusHistory_OnTrackedRequest_MustBeInserted()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
        var request = new FleetRequest { Id = Guid.NewGuid(), RequestNo = "VH-TEST" };
        db.Attach(request);
        var method = typeof(FleetRequestsController).GetMethod("AddHistory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var history = (FleetRequestStatusHistory)method.Invoke(Controller(db),
            [request, "DRAFT", "CANCELLED", "Fleet.RequestCancelled", Guid.NewGuid(), null, "duplicate"])!;
        db.ChangeTracker.DetectChanges();
        Assert.Equal(EntityState.Added, db.Entry(history).State);
        Assert.Equal(request.Id, history.FleetRequestId);
    }

    [Fact]
    public async Task DatabaseFailure_IsNotReportedAsConcurrencyConflict()
    {
        var failure = new DbUpdateException("Database constraint failure");
        await using var db = new FailingDb(failure);
        var controller = Controller(db);
        var actual = await Assert.ThrowsAsync<DbUpdateException>(() => Save(controller));
        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task ConcurrencyFailure_RemainsConflict()
    {
        await using var db = new FailingDb(new DbUpdateConcurrencyException("Concurrent update"));
        Assert.False(await Save(Controller(db)));
    }

    private static FleetRequestsController Controller(AppDbContext db) => new(db, null!, null!, null!)
    {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
    };

    private static Task<bool> Save(FleetRequestsController controller) =>
        (Task<bool>)typeof(FleetRequestsController).GetMethod("TrySave", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(controller, [CancellationToken.None])!;

    private sealed class FailingDb(Exception failure) : AppDbContext(
        new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options)
    {
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromException<int>(failure);
    }
}
